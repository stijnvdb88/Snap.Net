/*
    This file is part of Snap.Net
    Copyright (C) 2020  Stijn Van der Borght
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using SnapDotNet.ControlClient.JsonRpcData;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SnapDotNet.ControlClient
{
    /// <summary>
    /// This class is responsible for all back and forth with snapserver
    /// </summary>
    public class SnapcastClient : IDisposable
    {
        public bool IsDisposed { get; private set; }
        private TcpClient m_TcpClient = null;
        private NetworkStream m_Stream;
        private JsonRpc m_JsonRpc;

        private Task m_ConnectionCheckTask;
        private CancellationTokenSource m_ConnectionCheckCancellationTokenSource;

        public ServerData ServerData { get; private set; }

        public event Action OnServerUpdated;

        private string m_Ip;
        private int m_Port;

        private bool m_RetryingConnection = false;

        public static bool AutoReconnect { get; set; }

        private readonly Queue<Action> m_QueuedMessages = new Queue<Action>();

        /// <summary>
        /// Connects to the snapserver and sets up local RPC methods
        /// </summary>
        /// <param name="ip">IP address to connect to</param>
        /// <param name="port">port to connect on</param>
        /// <returns></returns>
        public async Task ConnectAsync(string ip, int port)
        {
            // connect
            ServerData = null;
            m_Ip = ip;
            m_Port = port;

            m_TcpClient = new TcpClient();
            try
            {
                await m_TcpClient.ConnectAsync(ip, port);

            }
            catch (SocketException e)
            {
                // connection failed - bail
                // logging this to debug because we don't want to spam the log file (connection gets retried indefinitely if it had previously succeeded)
                Debug("Socket exception: ", e.Message);
                if (m_RetryingConnection == false)
                {
                    _StartReconnectLoop();
                }
                return;
            }
            // attach StreamJsonRpc (NewLineDelimited)
            m_Stream = m_TcpClient.GetStream();

            // UnbatchingNewLineDelimitedMessageHandler adapted from UnbatchingWebSocketMessageHandler 
            // https://github.com/microsoft/vs-streamjsonrpc/compare/master...AArnott:sampleUnbatchingMessageHandler
            m_JsonRpc = new StreamJsonRpc.JsonRpc(new UnbatchingNewLineDelimitedMessageHandler(m_Stream, m_Stream));

            //m_JsonRpc.TraceSource.Switch.Level = SourceLevels.All; // uncomment if you need detailed json-rpc logs

            // register methods (must be done before listening starts)
            m_JsonRpc.AddLocalRpcMethod("Server.OnUpdate", new Action<JsonRpcData.ServerData>((server) =>
            {
                Debug("Received Server.OnUpdate - {0}", server);
                _ServerUpdated(server);
            }));

            m_JsonRpc.AddLocalRpcMethod("Client.OnVolumeChanged", new Action<string, JsonRpcData.Volume>((id, volume) =>
            {
                Debug("Received Client.OnVolumeChanged - id {0}, volume {1}", id, volume);
                _ClientVolumeChanged(id, volume);
            }));

            m_JsonRpc.AddLocalRpcMethod("Client.OnNameChanged", new Action<string, string>((id, name) =>
            {
                Debug("Received Client.OnNameChanged - id {0}, name {1}", id, name);
                _ClientNameChanged(id, name);
            }));

            m_JsonRpc.AddLocalRpcMethod("Client.OnLatencyChanged", new Action<string, int>((id, latency) =>
            {
                Debug("Received Client.OnLatencyChanged - id {0}, latency {1}", id, latency);
                _ClientLatencyChanged(id, latency);
            }));

            m_JsonRpc.AddLocalRpcMethod("Client.OnConnect", new Action<string, JsonRpcData.Client>((id, client) =>
            {
                // when the server's been down, we keep trying to reconnect (as does every other client)
                // when reconnection succeeds, we might see other clients' OnConnect calls come in
                // before our own Server.GetStatus has completed. In that case, we queue these
                // and execute them as soon as we've received the server data
                Debug("Received Client.OnConnect - id {0}, client {1}", id, client);
                if (ServerData != null)
                {
                    _ClientConnectedOrDisconnected(id, client);
                }
                else
                {
                    m_QueuedMessages.Enqueue(() =>
                    {
                        _ClientConnectedOrDisconnected(id, client);
                    });
                }
            }));

            m_JsonRpc.AddLocalRpcMethod("Client.OnDisconnect", new Action<string, JsonRpcData.Client>((id, client) =>
            {
                Debug("Received Client.OnDisconnect - id {0}, client {1}", id, client);
                _ClientConnectedOrDisconnected(id, client);
            }));

            m_JsonRpc.AddLocalRpcMethod("Group.OnMute", new Action<string, bool>((id, mute) =>
            {
                Debug("Received Group.OnMute - id {0}, mute {1}", id, mute);
                _GroupMuteChanged(id, mute);
            }));

            m_JsonRpc.AddLocalRpcMethod("Group.OnNameChanged", new Action<string, string>((id, name) =>
            {
                Debug("Received Group.OnNameChanged - id {0}, name {1}", id, name);
                _GroupNameChanged(id, name);
            }));

            m_JsonRpc.AddLocalRpcMethod("Group.OnStreamChanged", new Action<string, string>((id, stream_id) =>
            {
                Debug("Received Group.OnStreamChanged - id {0}, stream_id {1}", id, stream_id);
                _GroupStreamChanged(id, stream_id);
            }));

            m_JsonRpc.AddLocalRpcMethod("Stream.OnUpdate", new Action<string, Stream>((id, stream) =>
            {
                Debug("Received Stream.OnUpdate - id {0}, stream {1}", id, stream);
                _StreamUpdated(id, stream);
            }));


            m_JsonRpc.StartListening();
            // call Server.GetStatus to get all metadata
            Data result = await m_JsonRpc.InvokeAsync<JsonRpcData.Data>("Server.GetStatus");
            _ServerUpdated(result.server);

            // make sure we find out if connection drops
            _StartReconnectLoop();
        }

        private void _StartReconnectLoop()
        {
            if (AutoReconnect == true)
            {
                m_RetryingConnection = true;
                m_ConnectionCheckCancellationTokenSource = new CancellationTokenSource();
                m_ConnectionCheckTask = Task.Run(_CheckConnectionAsync, m_ConnectionCheckCancellationTokenSource.Token);
            }
        }

        private async Task _CheckConnectionAsync()
        {
            while (m_TcpClient.Connected == true)
            {
                await Task.Delay(100);
            }

            Info("connection lost!");
            if (ServerData != null)
            {
                ServerData.SERVER_Invalidate(); // make sure everyone know the server is no longer with us
                ServerData = null; // rip
            }

            if (m_JsonRpc != null)
            {
                m_JsonRpc.Dispose();
            }

            while (m_TcpClient.Connected == false) // checking for this setting again in case user disables it while we're already in the retry loop
            {
                if (AutoReconnect == true)
                {
                    await ConnectAsync(m_Ip, m_Port); // keep knocking on the door
                    await Task.Delay(100);
                }
                else
                {
                    m_RetryingConnection = false;
                }

            }

            Info("connection restored!");
            await _CheckConnectionAsync().ConfigureAwait(false); // restart this check
        }

        /// <summary>
        /// Gets a client by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private JsonRpcData.Client _GetClient(string id)
        {
            if (ServerData != null)
            {
                foreach (Group g in ServerData.groups)
                {
                    foreach (JsonRpcData.Client c in g.clients)
                    {
                        if (c.id == id)
                        {
                            return c;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a client by id, also indicates its group in out parameter
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private JsonRpcData.Client _GetClient(string id, out Group group)
        {
            group = null;
            if (ServerData != null)
            {
                foreach (Group g in ServerData.groups)
                {
                    foreach (JsonRpcData.Client c in g.clients)
                    {
                        if (c.id == id)
                        {
                            group = g;
                            return c;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a group by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private JsonRpcData.Group _GetGroup(string id)
        {
            if (ServerData != null)
            {
                foreach (Group g in ServerData.groups)
                {
                    if (g.id == id)
                    {
                        return g;
                    }
                }
            }
            return null;
        }

        public JsonRpcData.Stream GetStream(string id)
        {
            if (ServerData != null)
            {
                foreach (Stream stream in ServerData.streams)
                {
                    if (stream.id == id)
                    {
                        return stream;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Called when snapserver reports server update (= all data! requires a full ui reload)
        /// </summary>
        /// <param name="data"></param>
        private void _ServerUpdated(ServerData data)
        {
            if (ServerData != null)
            {
                // if we already had ServerData, make sure all UI elements know it's about to be chucked out
                ServerData.SERVER_Invalidate();
            }

            ServerData = data;

            // if any incoming messages got queued while we were waiting for ServerData, handle them now (see Client.OnConnect local rpc method)
            while (m_QueuedMessages.Count > 0)
            {
                m_QueuedMessages.Dequeue()();
            }

            // hook up events so we can update the server whenever data gets modified
            foreach (Group group in ServerData.groups)
            {
                foreach (JsonRpcData.Client client in group.clients)
                {
                    client.config.volume.CLIENT_OnModified += () =>
                    {
                        _OnLocalVolumeDataModified(client);
                    };
                    client.CLIENT_OnNameModified += () =>
                    {
                        _OnLocalClientNameModified(client);
                    };
                    client.config.CLIENT_OnLatencyModified += () =>
                    {
                        _OnLocalClientLatencyModified(client);
                    };
                    client.CLIENT_OnRemoved += () =>
                    {
                        _OnLocalClientRemoved(client);
                    };
                }
                group.SubscribeToClientEvents();

                group.CLIENT_OnMuteModified += () =>
                {
                    _OnLocalGroupMuteModified(group);
                };
                group.CLIENT_OnNameModified += () =>
                {
                    _OnLocalGroupNameModified(group);
                };
                group.CLIENT_OnStreamModified += () =>
                {
                    _OnLocalGroupStreamModified(group);
                };

                group.CLIENT_OnClientListModified += (clientIds) =>
                {
                    _OnLocalGroupClientListModified(group, clientIds);
                };
            }

            OnServerUpdated?.Invoke();
        }

        /// <summary>
        /// Called when snapserver reports client volume update
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="volume">new volume object</param>
        private void _ClientVolumeChanged(string id, Volume volume)
        {
            JsonRpcData.Group group = null;
            JsonRpcData.Client client = _GetClient(id, out group);
            if (client != null)
            {
                client.config.SERVER_SetVolume(volume);
                // groups are listening to the clients' volume object for change events,
                // in order to update the group volume slider. when receiving a volume
                // update from the client, the group needs to re-subscribe to this event
                group.SubscribeToClientEvent(client); 

                // also resubscribe local event for volume changes, so the server gets
                // updated when the user modifies the volume
                client.config.volume.CLIENT_OnModified += () =>
                {
                    _OnLocalVolumeDataModified(client);
                };
            }
        }

        /// <summary>
        /// Called when snapserver reports client name update
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="name">new name</param>
        private void _ClientNameChanged(string id, string name)
        {
            JsonRpcData.Client client = _GetClient(id);
            if (client != null)
            {
                client.SERVER_SetName(name);
            }
        }

        /// <summary>
        /// Called when snapserver reports client latency update
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="latency">new latency</param>
        private void _ClientLatencyChanged(string id, int latency)
        {
            JsonRpcData.Client client = _GetClient(id);
            if (client != null)
            {
                client.config.latency = latency;
            }
        }

        /// <summary>
        /// Called when snapserver reports group mute update
        /// </summary>
        /// <param name="id">group id</param>
        /// <param name="mute">new mute state</param>
        private void _GroupMuteChanged(string id, bool mute)
        {
            JsonRpcData.Group group = _GetGroup(id);
            if (group != null)
            {
                group.SERVER_SetMute(mute);
            }
        }

        /// <summary>
        /// Called when snapserver reports group name update
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="name">new name</param>
        private void _GroupNameChanged(string id, string name)
        {
            JsonRpcData.Group group = _GetGroup(id);
            if (group != null)
            {
                group.SERVER_SetName(name);
            }
        }

        /// <summary>
        /// Called when snapserver reports stream update
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="stream">new stream</param>
        private void _StreamUpdated(string id, Stream stream)
        {
            for (int i = 0; i < ServerData.streams.Length; i++)
            {
                if (ServerData.streams[i].id == id)
                {
                    ServerData.streams[i].SERVER_Update(stream);
                }
            }
        }

        /// <summary>
        /// Called when snapserver reports group stream update
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="name">new name</param>
        private void _GroupStreamChanged(string id, string stream_id)
        {
            JsonRpcData.Group group = _GetGroup(id);
            if (group != null)
            {
                group.SERVER_SetStream(stream_id);
            }
        }

        /// <summary>
        /// Called when snapserver reports client connect/disconnect
        /// </summary>
        /// <param name="client">the client</param>
        private void _ClientConnectedOrDisconnected(string id, JsonRpcData.Client client)
        {
            JsonRpcData.Client c = _GetClient(id);
            c.SERVER_SetConnected(client.connected);
            c.SERVER_SetLastSeen(client.lastSeen);
        }

        /// <summary>
        /// called when we locally modify group mute state (aka we need to notify the server)
        /// </summary>
        /// <param name="group"></param>
        private void _OnLocalGroupMuteModified(JsonRpcData.Group group)
        {
            Task t = Task.Run(async () =>
            {
                await _SetGroupMutedAsync(group.id, group.muted).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// called when we locally modify group's name (aka we need to notify the server)
        /// </summary>
        /// <param name="group"></param>
        private void _OnLocalGroupNameModified(JsonRpcData.Group group)
        {
            Task t = Task.Run(async () =>
            {
                await _SetGroupNameAsync(group.id, group.name).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// called when we locally modify group's stream_id (aka we need to notify the server)
        /// </summary>
        /// <param name="group"></param>
        private void _OnLocalGroupStreamModified(Group group)
        {
            Task t = Task.Run(async () =>
            {
                await _SetGroupStreamAsync(group.id, group.stream_id).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// called when we locally modify group's client list (aka we need to notify the server)
        /// </summary>
        /// <param name="group"></param>
        /// <param name="clientIds"></param>
        private void _OnLocalGroupClientListModified(Group group, string[] clientIds)
        {
            Task t = Task.Run(async () =>
            {
                await _SetGroupClientsAsync(group.id, clientIds).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// called when we locally modify client volume data (aka we need to notify the server)
        /// </summary>
        /// <param name="client"></param>
        private void _OnLocalVolumeDataModified(JsonRpcData.Client client)
        {
            Task t = Task.Run(async () =>
            {
                await _SetClientVolumeAsync(client.id, client.config.volume).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// called when we locally modify a client's name (aka we need to notify the server)
        /// </summary>
        /// <param name="client"></param>
        private void _OnLocalClientNameModified(JsonRpcData.Client client)
        {
            Task t = Task.Run(async () =>
            {
                await _SetClientNameAsync(client.id, client.config.name).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// called when we locally modify a client's latency (aka we need to notify the server)
        /// </summary>
        /// <param name="client"></param>
        private void _OnLocalClientLatencyModified(JsonRpcData.Client client)
        {
            Task t = Task.Run(async () =>
            {
                await _SetClientLatencyAsync(client.id, client.config.latency).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// called when we locally remove a client (aka we need to notify the server)
        /// </summary>
        /// <param name="client"></param>
        private void _OnLocalClientRemoved(JsonRpcData.Client client)
        {
            Task t = Task.Run(async () =>
            {
                await _RemoveClientAsync(client.id).ConfigureAwait(false);
            });
        }


        /// <summary>
        /// Called when we want to update a client's volume on snapserver
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="volume">new volume object</param>
        private async Task _SetClientVolumeAsync(string id, Volume volume)
        {
            Debug("Sending Client.SetVolume - id {0}, volume {1}", id, volume);
            ClientVolumeCommandResult result = await m_JsonRpc.InvokeWithParameterObjectAsync<ClientVolumeCommandResult>("Client.SetVolume",
                new ClientVolumeCommand() { id = id, volume = volume }); // update the server
            _ClientVolumeChanged(id, result.volume); // update our local data to reflect the new value
        }

        /// <summary>
        /// Called when we want to update a client's name on snapserver
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="name">new name</param>
        private async Task _SetClientNameAsync(string id, string name)
        {
            Debug("Sending Client.SetName - id {0}, name {1}", id, name);
            SetNameCommandResult result = await m_JsonRpc.InvokeWithParameterObjectAsync<SetNameCommandResult>("Client.SetName",
                new NameCommand() { id = id, name = name }); // update the server
            _ClientNameChanged(id, result.name); // update our local data to reflect the new value
        }

        /// <summary>
        /// Called when we want to update a client's latency on snapserver
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="latency">new latency</param>
        private async Task _SetClientLatencyAsync(string id, int latency)
        {
            Debug("Sending Client.SetLatency - id {0}, latency {1}", id, latency);
            ClientVolumeCommandResult result = await m_JsonRpc.InvokeWithParameterObjectAsync<ClientVolumeCommandResult>("Client.SetLatency",
                new ClientLatencyCommand() { id = id, latency = latency }); // update the server
            _ClientLatencyChanged(id, latency); // update our local data to reflect the new value
        }

        /// <summary>
        /// Called when we want to remove a client from snapserver
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="latency">new latency</param>
        private async Task _RemoveClientAsync(string id)
        {
            Debug("Sending Server.DeleteClient - id {0}", id);
            ServerUpdateCommandResult result = await m_JsonRpc.InvokeWithParameterObjectAsync<ServerUpdateCommandResult>("Server.DeleteClient",
                new RemoveClientCommand() { id = id }); // update the server
            _ServerUpdated(result.server);
        }

        /// <summary>
        /// Called when we want to update a group's muted state on snapserver
        /// </summary>
        /// <param name="id">group id</param>
        /// <param name="muted">new muted state</param>
        private async Task _SetGroupMutedAsync(string id, bool mute)
        {
            Debug("Sending Group.SetMute - id {0}, mute {1}", id, mute);
            SetMutedCommandResult result = await m_JsonRpc.InvokeWithParameterObjectAsync<SetMutedCommandResult>("Group.SetMute",
                new GroupSetMutedCommand() { id = id, mute = mute }); // update the server
            _GroupMuteChanged(id, result.mute);
        }

        /// <summary>
        /// Called when we want to update a group's name on snapserver
        /// </summary>
        /// <param name="id">group id</param>
        /// <param name="name">new name</param>
        private async Task _SetGroupNameAsync(string id, string name)
        {
            Debug("Sending Group.SetName - id {0}, name {1}", id, name);
            SetNameCommandResult result = await m_JsonRpc.InvokeWithParameterObjectAsync<SetNameCommandResult>("Group.SetName",
                new NameCommand() { id = id, name = name }); // update the server
            _GroupNameChanged(id, result.name); // update our local data to reflect the new value
        }

        /// <summary>
        /// Called when we want to update a group's stream_id on snapserver
        /// </summary>
        /// <param name="id">group id</param>
        /// <param name="stream_id">new stream_id</param>
        private async Task _SetGroupStreamAsync(string id, string stream_id)
        {
            Debug("Sending Group.SetStream - id {0}, stream_id {1}", id, stream_id);
            SetStreamCommandResult result = await m_JsonRpc.InvokeWithParameterObjectAsync<SetStreamCommandResult>("Group.SetStream",
                new StreamCommand() { id = id, stream_id = stream_id }); // update the server
            _GroupNameChanged(id, result.stream_id); // update our local data to reflect the new value
        }

        /// <summary>
        /// Called when we want to update a group's client list on snapserver
        /// </summary>
        /// <param name="id">group id</param>
        /// <param name="clientIds">client ids</param>
        private async Task _SetGroupClientsAsync(string id, string[] clientIds)
        {
            Debug("Sending Group.SetClients - id {0}, clients {1}", id, string.Join(",", clientIds));
            ServerUpdateCommandResult result = await m_JsonRpc.InvokeWithParameterObjectAsync<ServerUpdateCommandResult>("Group.SetClients",
                new GroupSetClientsCommand() { id = id, clients = clientIds }); // update the server)
            _ServerUpdated(result.server); // update our local data
        }


        private static void Debug(string message, params object[] args)
        {
            // Logger.Debug(message, args);
        }

        private static void Info(string message, params object[] args)
        {
            // Logger.Info(message, args);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            m_ConnectionCheckCancellationTokenSource.Cancel();
            if (m_ConnectionCheckTask != null)
            {
                if (m_ConnectionCheckTask.Status == TaskStatus.RanToCompletion
                    || m_ConnectionCheckTask.Status == TaskStatus.Faulted
                    || m_ConnectionCheckTask.Status == TaskStatus.Canceled)
                    m_ConnectionCheckTask.Dispose();
            }

            m_JsonRpc.Dispose();
            m_Stream.Dispose();
            m_TcpClient.Dispose();
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                this.IsDisposed = true;
            }
        }
    }
}
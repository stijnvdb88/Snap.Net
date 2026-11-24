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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Snap.Net.ControlClient.JsonRpcData;

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

        // One gate owns connection replacement and outgoing RPCs. In particular, a
        // slider must not race a reconnect or apply replies out of order.
        private readonly SemaphoreSlim m_ConnectionGate = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource m_Lifetime = new CancellationTokenSource();
        private readonly object m_CommandLock = new object();
        private Task m_CommandTask = Task.CompletedTask;
        private Task m_ConnectionCheckTask;
        private Task m_ReconnectTask;
        private int m_Timeout = 3000;

        public ServerData ServerData { get; private set; }

        public event Action OnServerUpdated;

        private string m_Ip;
        private int m_Port;

        public static bool AutoReconnect { get; set; }

        public bool ConnectionFailed { get; private set; }

        /// <summary>
        /// Connects to the snapserver and sets up local RPC methods
        /// </summary>
        /// <param name="ip">IP address to connect to</param>
        /// <param name="port">port to connect on</param>
        /// <returns></returns>
        public async Task ConnectAsync(string ip, int port, int timeout = 3000)
        {
            if (timeout <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            try
            {
                await m_ConnectionGate.WaitAsync(m_Lifetime.Token).ConfigureAwait(false);
                try
                {
                    if (IsDisposed)
                        return;
                    m_Ip = ip;
                    m_Port = port;
                    m_Timeout = timeout;
                    await _ConnectAsync().ConfigureAwait(false);
                    _StartReconnectLoop();
                }
                finally
                {
                    m_ConnectionGate.Release();
                }
            }
            catch (OperationCanceledException) when (m_Lifetime.IsCancellationRequested) { }
        }

        /// <summary>
        /// Replaces the control connection after a host/network interruption. Concurrent
        /// requests share one attempt. Does not restart playback or broadcast.
        /// </summary>
        public Task ReconnectAsync()
        {
            lock (m_CommandLock)
            {
                if (IsDisposed || string.IsNullOrEmpty(m_Ip))
                    return Task.CompletedTask;
                if (m_ReconnectTask == null || m_ReconnectTask.IsCompleted)
                    m_ReconnectTask = Task.Run(_ReconnectAsync);
                return m_ReconnectTask;
            }
        }

        private async Task _ReconnectAsync()
        {
            try
            {
                Info("Control reconnect requested after host/network interruption.");
                // Abort an in-flight request before waiting for the connection gate.
                // A silent pre-resume socket must not delay recovery until its timeout.
                var rpc = m_JsonRpc;
                rpc?.Dispose();
                await m_ConnectionGate.WaitAsync(m_Lifetime.Token).ConfigureAwait(false);
                try
                {
                    if (!IsDisposed)
                    {
                        await _ConnectAsync().ConfigureAwait(false);
                        _StartReconnectLoop();
                    }
                }
                finally { m_ConnectionGate.Release(); }
            }
            catch (OperationCanceledException) when (m_Lifetime.IsCancellationRequested) { }
            catch (Exception e) { Info("Control reconnect failed: {0}", e); }
        }

        private async Task _ConnectAsync()
        {
            _Disconnect();
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            Info("Connecting control to {0}:{1} (timeout {2} ms).", m_Ip, m_Port, m_Timeout);
            var tcpClient = new TcpClient();
            m_TcpClient = tcpClient;
            try
            {
                using (m_Lifetime.Token.Register(tcpClient.Dispose))
                {
                    var connect = tcpClient.ConnectAsync(m_Ip, m_Port);
                    if (await Task.WhenAny(connect, Task.Delay(m_Timeout, m_Lifetime.Token)).ConfigureAwait(false) != connect)
                    {
                        tcpClient.Dispose();
                        // DNS may still be completing: observe it without extending
                        // the caller's deadline after the socket has been closed.
                        _ = _ObserveRequestAsync(connect);
                        throw new TimeoutException("Snapcast control connection timed out.");
                    }
                    await connect.ConfigureAwait(false);
                }
                m_Lifetime.Token.ThrowIfCancellationRequested();
                await _InitializeRpcAsync(tcpClient).ConfigureAwait(false);
                Info("Control connection ready at {0}:{1} after {2} ms.", m_Ip, m_Port, elapsed.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                Info("Control connection to {0}:{1} failed after {2} ms: {3}: {4}",
                    m_Ip, m_Port, elapsed.ElapsedMilliseconds, e.GetType().Name, e.Message);
                _Disconnect();
            }
        }

        private async Task _InitializeRpcAsync(TcpClient tcpClient)
        {
            // connect
            ConnectionFailed = false;
            ServerData = null;

            // attach StreamJsonRpc (NewLineDelimited)
            m_Stream = tcpClient.GetStream();

            // UnbatchingNewLineDelimitedMessageHandler adapted from UnbatchingWebSocketMessageHandler 
            // https://github.com/microsoft/vs-streamjsonrpc/compare/master...AArnott:sampleUnbatchingMessageHandler
            m_JsonRpc = new StreamJsonRpc.JsonRpc(new UnbatchingNewLineDelimitedMessageHandler(m_Stream, m_Stream));

            var rpc = m_JsonRpc;

            //m_JsonRpc.TraceSource.Switch.Level = SourceLevels.All; // uncomment if you need detailed json-rpc logs

            // register methods (must be done before listening starts)
            m_JsonRpc.AddLocalRpcMethod("Server.OnUpdate", new Func<JsonRpcData.ServerData, Task>((server) => _HandleNotificationAsync(rpc, () =>
            {
                Debug("Received Server.OnUpdate - {0}", server);
                _ServerUpdated(server);
            })));

            m_JsonRpc.AddLocalRpcMethod("Client.OnVolumeChanged", new Func<string, JsonRpcData.Volume, Task>(
                (id, volume) => _HandleVolumeNotificationAsync(rpc, id, volume)));

            m_JsonRpc.AddLocalRpcMethod("Client.OnNameChanged", new Func<string, string, Task>((id, name) => _HandleNotificationAsync(rpc, () =>
            {
                Debug("Received Client.OnNameChanged - id {0}, name {1}", id, name);
                _ClientNameChanged(id, name);
            })));

            m_JsonRpc.AddLocalRpcMethod("Client.OnLatencyChanged", new Func<string, int, Task>((id, latency) => _HandleNotificationAsync(rpc, () =>
            {
                Debug("Received Client.OnLatencyChanged - id {0}, latency {1}", id, latency);
                _ClientLatencyChanged(id, latency);
            })));

            m_JsonRpc.AddLocalRpcMethod("Client.OnConnect", new Func<string, JsonRpcData.Client, Task>((id, client) => _HandleNotificationAsync(rpc, () =>
            {
                _ClientConnectedOrDisconnected(id, client);
            })));

            m_JsonRpc.AddLocalRpcMethod("Client.OnDisconnect", new Func<string, JsonRpcData.Client, Task>((id, client) => _HandleNotificationAsync(rpc, () =>
            {
                Debug("Received Client.OnDisconnect - id {0}, client {1}", id, client);
                _ClientConnectedOrDisconnected(id, client);
            })));

            m_JsonRpc.AddLocalRpcMethod("Group.OnMute", new Func<string, bool, Task>(
                (id, mute) => _HandleMuteNotificationAsync(rpc, id, mute)));

            m_JsonRpc.AddLocalRpcMethod("Group.OnNameChanged", new Func<string, string, Task>((id, name) => _HandleNotificationAsync(rpc, () =>
            {
                Debug("Received Group.OnNameChanged - id {0}, name {1}", id, name);
                _GroupNameChanged(id, name);
            })));

            m_JsonRpc.AddLocalRpcMethod("Group.OnStreamChanged", new Func<string, string, Task>((id, stream_id) => _HandleNotificationAsync(rpc, () =>
            {
                Debug("Received Group.OnStreamChanged - id {0}, stream_id {1}", id, stream_id);
                _GroupStreamChanged(id, stream_id);
            })));

            m_JsonRpc.AddLocalRpcMethod("Stream.OnUpdate", new Func<string, Stream, Task>((id, stream) => _HandleNotificationAsync(rpc, () =>
            {
                Debug("Received Stream.OnUpdate - id {0}, stream {1}", id, stream);
                _StreamUpdated(id, stream);
            })));

            m_JsonRpc.AddLocalRpcMethod("Stream.OnProperties", new Func<string, Properties, Task>((id, properties) => _HandleNotificationAsync(rpc, () =>
            {
                Debug("Received Stream.OnProperties - id {0}, properties {1}", id, properties);
                _StreamPropertiesUpdated(id, properties);
            })));

            m_JsonRpc.StartListening();
            // call Server.GetStatus to get all metadata
            await _GetServerStatusAsync().ConfigureAwait(false);
        }

        public bool IsConnected()
        {
            var rpc = m_JsonRpc;
            return !IsDisposed && ServerData != null && rpc != null && !rpc.Completion.IsCompleted;
        }

        private async Task _HandleNotificationAsync(JsonRpc rpc, Action notification)
        {
            try
            {
                // Notifications can arrive before GetStatus finishes, or remain queued
                // after reconnect. Process them only against their own connection.
                await m_ConnectionGate.WaitAsync(m_Lifetime.Token).ConfigureAwait(false);
                try
                {
                    if (!IsDisposed && ReferenceEquals(rpc, m_JsonRpc))
                        notification();
                }
                finally { m_ConnectionGate.Release(); }
            }
            catch (OperationCanceledException) when (m_Lifetime.IsCancellationRequested) { }
        }

        private Task _HandleVolumeNotificationAsync(JsonRpc rpc, string id, Volume value)
        {
            var volume = _GetClient(id)?.config.volume;
            if (volume == null)
                return _HandleNotificationAsync(rpc, () => _ClientVolumeChanged(id, value));
            long revision;
            bool pending;
            lock (volume.SyncRoot)
            {
                volume.ServerRevision++;
                revision = volume.LocalRevision;
                pending = volume.PendingChange != null;
            }
            return _HandleNotificationAsync(rpc, () =>
            {
                lock (volume.SyncRoot)
                {
                    // Echoes queued during a local burst cannot undo it after drain.
                    // The final command explicitly reads authoritative server state.
                    if (!pending && revision == volume.LocalRevision)
                        _ClientVolumeChanged(id, value);
                }
            });
        }

        private Task _HandleMuteNotificationAsync(JsonRpc rpc, string id, bool value)
        {
            var group = _GetGroup(id);
            if (group == null)
                return _HandleNotificationAsync(rpc, () => _GroupMuteChanged(id, value));
            long revision;
            bool pending;
            lock (group.MuteSyncRoot)
            {
                group.ServerMuteRevision++;
                revision = group.LocalMuteRevision;
                pending = group.PendingMuteChange != null;
            }
            return _HandleNotificationAsync(rpc, () =>
            {
                if (!pending)
                {
                    var current = _GetGroup(id);
                    if (ReferenceEquals(current, group))
                        current.SERVER_SetMute(value, revision);
                    else
                        current?.SERVER_SetMute(value);
                }
            });
        }

        // Caller holds m_ConnectionGate.
        private void _Disconnect()
        {
            if (m_JsonRpc != null || ServerData != null)
                Info("Closing control connection to {0}:{1}; invalidating local server state.", m_Ip, m_Port);
            ConnectionFailed = true;
            var rpc = m_JsonRpc;
            m_JsonRpc = null;
            rpc?.Dispose();
            // Completion can fault even when no request was in flight.
            if (rpc != null)
                _ = _ObserveCompletionAsync(rpc);
            m_Stream?.Dispose();
            m_Stream = null;
            m_TcpClient?.Dispose();
            m_TcpClient = null;
            var data = ServerData;
            ServerData = null;
            if (data != null && !IsDisposed)
            {
                data.SERVER_Invalidate();
                OnServerUpdated?.Invoke();
            }
        }

        public async Task GetServerStatusAsync()
        {
            try
            {
                await m_ConnectionGate.WaitAsync(m_Lifetime.Token).ConfigureAwait(false);
                try
                {
                    if (m_JsonRpc != null)
                        await _GetServerStatusAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Info("Server.GetStatus failed: {0}: {1}", e.GetType().Name, e.Message);
                    _Disconnect();
                }
                finally
                {
                    m_ConnectionGate.Release();
                }
            }
            catch (OperationCanceledException) when (m_Lifetime.IsCancellationRequested) { }
        }

        private async Task _GetServerStatusAsync()
        {
            Info("Requesting full control server status.");
            Data result = await _InvokeAsync<Data>("Server.GetStatus", null).ConfigureAwait(false);
            m_Lifetime.Token.ThrowIfCancellationRequested();
            _ServerUpdated(result.server);
        }

        private async Task<T> _InvokeAsync<T>(string method, object parameters)
        {
            var rpc = m_JsonRpc;
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(m_Lifetime.Token))
            {
                var request = rpc.InvokeWithParameterObjectAsync<T>(method, parameters, m_Lifetime.Token);
                if (await Task.WhenAny(request, Task.Delay(m_Timeout, deadline.Token)).ConfigureAwait(false) != request)
                {
                    // RPC cancellation alone can wait for cooperation from the peer.
                    // Close the transport to enforce a local deadline even on silent TCP.
                    rpc.Dispose();
                    _ = _ObserveRequestAsync(request);
                    m_Lifetime.Token.ThrowIfCancellationRequested();
                    Info("Control RPC {0} timed out after {1} ms at {2}:{3}.", method, m_Timeout, m_Ip, m_Port);
                    throw new TimeoutException("Snapcast control request timed out: " + method);
                }
                deadline.Cancel();
                var result = await request.ConfigureAwait(false);
                m_Lifetime.Token.ThrowIfCancellationRequested();
                return result;
            }
        }

        private static async Task _ObserveRequestAsync(Task request)
        {
            try { await request.ConfigureAwait(false); }
            catch (Exception e) { Debug("Control request ended: {0}", e.Message); }
        }

        private void _StartReconnectLoop()
        {
            if (m_ConnectionCheckTask == null && !IsDisposed)
                m_ConnectionCheckTask = Task.Run(_CheckConnectionAsync);
        }

        private static async Task _ObserveCompletionAsync(JsonRpc rpc)
        {
            try { await rpc.Completion.ConfigureAwait(false); }
            catch (Exception e) { Info("Control listener stopped: {0}: {1}", e.GetType().Name, e.Message); }
        }

        private async Task _CheckConnectionAsync()
        {
            while (!m_Lifetime.IsCancellationRequested)
            {
                try
                {
                    var rpc = m_JsonRpc;
                    if (rpc != null)
                        await _ObserveCompletionAsync(rpc).ConfigureAwait(false);

                    await m_ConnectionGate.WaitAsync(m_Lifetime.Token).ConfigureAwait(false);
                    try
                    {
                        // An explicit ConnectAsync may already have replaced this connection.
                        if (rpc != null && ReferenceEquals(rpc, m_JsonRpc))
                        {
                            Info("Control listener completed; recovering connection.");
                            _Disconnect();
                        }
                    }
                    finally { m_ConnectionGate.Release(); }

                    // No polling or heartbeat traffic while a connection is healthy.
                    if (IsConnected())
                        continue;
                    await Task.Delay(1000, m_Lifetime.Token).ConfigureAwait(false);
                    await m_ConnectionGate.WaitAsync(m_Lifetime.Token).ConfigureAwait(false);
                    try
                    {
                        if (AutoReconnect && !IsConnected() && !IsDisposed)
                            await _ConnectAsync().ConfigureAwait(false);
                    }
                    finally { m_ConnectionGate.Release(); }
                }
                catch (OperationCanceledException) when (m_Lifetime.IsCancellationRequested) { return; }
                catch (Exception e)
                {
                    // A listener/UI callback must not silently kill the only recovery worker.
                    Info("Control recovery failed: {0}", e);
                    try { await Task.Delay(1000, m_Lifetime.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) when (m_Lifetime.IsCancellationRequested) { return; }
                }
            }
        }

        private void _QueueCommand(Func<Task> command, Action completed = null)
        {
            lock (m_CommandLock)
            {
                // Capture the connection now; never replay an old UI action on
                // a replacement connection (especially a non-idempotent stream command).
                var previous = m_CommandTask;
                var rpc = m_JsonRpc;
                m_CommandTask = Task.Run(() => _RunCommandAsync(previous, rpc, command, completed));
            }
        }

        private async Task _RunCommandAsync(Task previous, JsonRpc rpc, Func<Task> command, Action completed)
        {
            try
            {
                await previous.ConfigureAwait(false);
                await m_ConnectionGate.WaitAsync(m_Lifetime.Token).ConfigureAwait(false);
                try
                {
                    if (rpc == null || !ReferenceEquals(rpc, m_JsonRpc) || !IsConnected())
                        return;
                    try
                    {
                        await command().ConfigureAwait(false);
                    }
                    catch (RemoteRpcException e)
                    {
                        Info("Control command rejected; refreshing server state: {0}", e.Message);
                        // A valid RPC error is not a dead connection; undo optimistic UI state.
                        await _GetServerStatusAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    Info("Control command failed: {0}: {1}", e.GetType().Name, e.Message);
                    _Disconnect();
                }
                finally { m_ConnectionGate.Release(); }
            }
            catch (OperationCanceledException) when (m_Lifetime.IsCancellationRequested) { }
            catch (Exception e) { Info("Control command failed: {0}", e); }
            finally { completed?.Invoke(); }
        }

        /// <summary>
        /// Gets a client by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private JsonRpcData.Client _GetClient(string id)
        {
            var data = ServerData;
            if (data != null)
            {
                foreach (Group g in data.groups)
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
            var data = ServerData;
            if (data != null)
            {
                foreach (Group g in data.groups)
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
            var data = ServerData;
            if (data != null)
            {
                foreach (Group g in data.groups)
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
            var data = ServerData;
            if (data != null)
            {
                foreach (Stream stream in data.streams)
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
            var previous = ServerData;
            if (previous != null)
            {
                // if we already had ServerData, make sure all UI elements know it's about to be chucked out
                previous.SERVER_Invalidate();
            }

            lock (m_CommandLock)
            {
                if (IsDisposed)
                    return;
                ServerData = data;
            }

            // hook up events so we can update the server whenever data gets modified
            foreach (Group group in data.groups)
            {
                foreach (JsonRpcData.Client client in group.clients)
                {
                    client.config.volume.CLIENT_OnModified += () => { if (ReferenceEquals(data, ServerData)) _OnLocalVolumeDataModified(client); };
                    client.CLIENT_OnNameModified += () => { if (ReferenceEquals(data, ServerData)) _OnLocalClientNameModified(client); };
                    client.config.CLIENT_OnLatencyModified += () => { if (ReferenceEquals(data, ServerData)) _OnLocalClientLatencyModified(client); };
                    client.CLIENT_OnRemoved += () => { if (ReferenceEquals(data, ServerData)) _OnLocalClientRemoved(client); };
                }

                group.SubscribeToClientEvents();

                group.CLIENT_OnMuteModified += () => { if (ReferenceEquals(data, ServerData)) _OnLocalGroupMuteModified(group); };
                group.CLIENT_OnNameModified += () => { if (ReferenceEquals(data, ServerData)) _OnLocalGroupNameModified(group); };
                group.CLIENT_OnStreamModified += () => { if (ReferenceEquals(data, ServerData)) _OnLocalGroupStreamModified(group); };

                group.CLIENT_OnClientListModified += (clientIds) =>
                {
                    if (ReferenceEquals(data, ServerData))
                        _OnLocalGroupClientListModified(group, clientIds);
                };
            }

            foreach (Stream stream in data.streams)
            {
                stream.CLIENT_OnControlCommand += command => { if (ReferenceEquals(data, ServerData)) _OnStreamControlCommandSent(stream, command); };
            }

            Info("Control state synchronized: {0} groups, {1} streams.", data.groups.Length, data.streams.Length);
            foreach (Group group in data.groups)
            {
                Info("Control status group {0}: muted={1}.", group.id, group.muted);
                foreach (JsonRpcData.Client client in group.clients)
                    Info("Control status client {0}: percent={1}, muted={2}, connected={3}.",
                        client.id, client.config.volume.percent, client.config.volume.muted, client.connected);
            }
            OnServerUpdated?.Invoke();
        }

        /// <summary>
        /// Called when snapserver reports client volume update
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="volume">new volume object</param>\
        private void _ClientVolumeChanged(string id, Volume volume)
        {
            JsonRpcData.Client client = _GetClient(id);
            if (client != null)
            {
                client.config.SERVER_SetVolume(volume);
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
            var data = ServerData;
            if (data == null)
                return;
            for (int i = 0; i < data.streams.Length; i++)
            {
                if (data.streams[i].id == id)
                {
                    data.streams[i].SERVER_Update(stream);
                }
            }
        }

        /// <summary>
        /// Called when snapserver reports stream properties update
        /// </summary>
        /// <param name="id"></param>
        /// <param name="properties"></param>
        private void _StreamPropertiesUpdated(string id, Properties properties)
        {
            var data = ServerData;
            if (data == null)
                return;
            for (int i = 0; i < data.streams.Length; i++)
            {
                if (data.streams[i].id == id)
                {
                    data.streams[i].SERVER_PropertiesUpdate(properties);
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
            if (c == null)
                return;
            c.SERVER_SetConnected(client.connected);
            c.SERVER_SetLastSeen(client.lastSeen);
        }

        /// <summary>
        /// called when we locally modify group mute state (aka we need to notify the server)
        /// </summary>
        /// <param name="group"></param>
        private void _OnLocalGroupMuteModified(JsonRpcData.Group group)
        {
            var change = new object();
            string id = group.id;
            bool muted = group.muted;
            group.PendingMuteChange = change; // caller holds MuteSyncRoot
            _QueueCommand(() => _SetGroupMutedAsync(id, muted, group, change), () =>
            {
                lock (group.MuteSyncRoot)
                    if (ReferenceEquals(group.PendingMuteChange, change))
                        group.PendingMuteChange = null;
            });
        }

        /// <summary>
        /// called when we locally modify group's name (aka we need to notify the server)
        /// </summary>
        /// <param name="group"></param>
        private void _OnLocalGroupNameModified(JsonRpcData.Group group)
        {
            string id = group.id;
            string name = group.name;
            _QueueCommand(() => _SetGroupNameAsync(id, name));
        }

        /// <summary>
        /// called when we locally modify group's stream_id (aka we need to notify the server)
        /// </summary>
        /// <param name="group"></param>
        private void _OnLocalGroupStreamModified(Group group)
        {
            string id = group.id;
            string streamId = group.stream_id;
            _QueueCommand(() => _SetGroupStreamAsync(id, streamId));
        }

        /// <summary>
        /// called when we locally modify group's client list (aka we need to notify the server)
        /// </summary>
        /// <param name="group"></param>
        /// <param name="clientIds"></param>
        private void _OnLocalGroupClientListModified(Group group, string[] clientIds)
        {
            string id = group.id;
            var ids = (string[])clientIds.Clone();
            _QueueCommand(() => _SetGroupClientsAsync(id, ids));
        }

        /// <summary>
        /// called when we locally modify client volume data (aka we need to notify the server)
        /// </summary>
        /// <param name="client"></param>
        private void _OnLocalVolumeDataModified(JsonRpcData.Client client)
        {
            var source = client.config.volume; // caller holds source.SyncRoot
            var volume = new Volume { percent = source.percent, muted = source.muted };
            string id = client.id;
            source.PendingChange = volume;
            _QueueCommand(() => _SetClientVolumeAsync(id, volume, client), () =>
            {
                lock (source.SyncRoot)
                    if (ReferenceEquals(source.PendingChange, volume))
                        source.PendingChange = null;
            });
        }

        /// <summary>
        /// called when we locally modify a client's name (aka we need to notify the server)
        /// </summary>
        /// <param name="client"></param>
        private void _OnLocalClientNameModified(JsonRpcData.Client client)
        {
            string id = client.id;
            string name = client.config.name;
            _QueueCommand(() => _SetClientNameAsync(id, name));
        }

        /// <summary>
        /// called when we locally modify a client's latency (aka we need to notify the server)
        /// </summary>
        /// <param name="client"></param>
        private void _OnLocalClientLatencyModified(JsonRpcData.Client client)
        {
            string id = client.id;
            int latency = client.config.latency;
            _QueueCommand(() => _SetClientLatencyAsync(id, latency));
        }

        /// <summary>
        /// called when we locally remove a client (aka we need to notify the server)
        /// </summary>
        /// <param name="client"></param>
        private void _OnLocalClientRemoved(JsonRpcData.Client client)
        {
            string id = client.id;
            _QueueCommand(() => _RemoveClientAsync(id));
        }

        /// <summary>
        /// Called when we locally send a control command to the stream (play/pause/next/prev/etc)
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="command"></param>
        private void _OnStreamControlCommandSent(JsonRpcData.Stream stream, Stream.EControlCommand command)
        {
            string id = stream.id;
            string value = command.ToString();
            _QueueCommand(() => _SendStreamControlCommandAsync(id, value));
        }

        /// <summary>
        /// Called when we want to update a client's volume on snapserver
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="volume">new volume object</param>
        private async Task _SetClientVolumeAsync(string id, Volume volume, JsonRpcData.Client client)
        {
            Debug("Sending Client.SetVolume - id {0}, volume {1}", id, volume);
            await _InvokeAsync<ClientVolumeCommandResult>("Client.SetVolume",
                    new ClientVolumeCommand() {id = id, volume = volume}); // update the server
            await _ReconcileClientVolumeAsync(id, volume, client).ConfigureAwait(false);
        }

        private async Task _ReconcileClientVolumeAsync(string id, Volume volume, JsonRpcData.Client client)
        {
            var source = client.config.volume;
            long revision;
            lock (source.SyncRoot)
            {
                if (!ReferenceEquals(source.PendingChange, volume))
                    return;
                revision = source.ServerRevision;
            }
            // A read after the last write includes external changes made during the
            // burst. It updates this volume only, without rebuilding the WPF controls.
            var status = await _InvokeAsync<ClientStatusResult>("Client.GetStatus", new { id }).ConfigureAwait(false);
            lock (source.SyncRoot)
            {
                if (ReferenceEquals(source.PendingChange, volume))
                {
                    if (source.ServerRevision != revision)
                    {
                        // A notification arrived while the snapshot was in flight.
                        // Re-read at the queue tail: never hold the gate in a retry loop
                        // or treat an older snapshot as newer than that notification.
                        var next = new Volume { percent = source.percent, muted = source.muted };
                        source.PendingChange = next;
                        _QueueCommand(() => _ReconcileClientVolumeAsync(id, next, client), () =>
                        {
                            lock (source.SyncRoot)
                                if (ReferenceEquals(source.PendingChange, next)) source.PendingChange = null;
                        });
                        return;
                    }
                    source.PendingChange = null;
                    _GetClient(id)?.config.SERVER_SetVolume(status.client.config.volume);
                }
            }
        }

        /// <summary>
        /// Called when we want to update a client's name on snapserver
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="name">new name</param>
        private async Task _SetClientNameAsync(string id, string name)
        {
            Debug("Sending Client.SetName - id {0}, name {1}", id, name);
            SetNameCommandResult result = await _InvokeAsync<SetNameCommandResult>(
                "Client.SetName",
                new NameCommand() {id = id, name = name}); // update the server
            if (_GetClient(id)?.config.name == name)
                _ClientNameChanged(id, result.name);
        }

        /// <summary>
        /// Called when we want to update a client's latency on snapserver
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="latency">new latency</param>
        private async Task _SetClientLatencyAsync(string id, int latency)
        {
            Debug("Sending Client.SetLatency - id {0}, latency {1}", id, latency);
            ClientLatencyCommandResult result =
                await _InvokeAsync<ClientLatencyCommandResult>("Client.SetLatency",
                    new ClientLatencyCommand() {id = id, latency = latency}); // update the server
            if (_GetClient(id)?.config.latency == latency)
                _ClientLatencyChanged(id, result.latency);
        }

        /// <summary>
        /// Called when we want to remove a client from snapserver
        /// </summary>
        /// <param name="id">client id</param>
        /// <param name="latency">new latency</param>
        private async Task _RemoveClientAsync(string id)
        {
            Debug("Sending Server.DeleteClient - id {0}", id);
            ServerUpdateCommandResult result =
                await _InvokeAsync<ServerUpdateCommandResult>("Server.DeleteClient",
                    new RemoveClientCommand() {id = id}); // update the server
            _ServerUpdated(result.server);
        }

        /// <summary>
        /// Called when we want to update a group's muted state on snapserver
        /// </summary>
        /// <param name="id">group id</param>
        /// <param name="muted">new muted state</param>
        private async Task _SetGroupMutedAsync(string id, bool mute, Group group, object change)
        {
            Debug("Sending Group.SetMute - id {0}, mute {1}", id, mute);
            await _InvokeAsync<SetMutedCommandResult>(
                "Group.SetMute",
                new GroupSetMutedCommand() {id = id, mute = mute}); // update the server
            await _ReconcileGroupMuteAsync(id, group, change).ConfigureAwait(false);
        }

        private async Task _ReconcileGroupMuteAsync(string id, Group group, object change)
        {
            long revision;
            lock (group.MuteSyncRoot)
            {
                if (!ReferenceEquals(group.PendingMuteChange, change))
                    return;
                revision = group.ServerMuteRevision;
            }
            var status = await _InvokeAsync<GroupStatusResult>("Group.GetStatus", new { id }).ConfigureAwait(false);
            lock (group.MuteSyncRoot)
            {
                if (!ReferenceEquals(group.PendingMuteChange, change))
                    return;
                if (group.ServerMuteRevision != revision)
                {
                    var next = new object();
                    group.PendingMuteChange = next;
                    _QueueCommand(() => _ReconcileGroupMuteAsync(id, group, next), () =>
                    {
                        lock (group.MuteSyncRoot)
                            if (ReferenceEquals(group.PendingMuteChange, next)) group.PendingMuteChange = null;
                    });
                    return;
                }
                group.PendingMuteChange = null;
            }
            _GetGroup(id)?.SERVER_SetMute(status.group.muted);
        }

        /// <summary>
        /// Called when we want to update a group's name on snapserver
        /// </summary>
        /// <param name="id">group id</param>
        /// <param name="name">new name</param>
        private async Task _SetGroupNameAsync(string id, string name)
        {
            Debug("Sending Group.SetName - id {0}, name {1}", id, name);
            SetNameCommandResult result = await _InvokeAsync<SetNameCommandResult>(
                "Group.SetName",
                new NameCommand() {id = id, name = name}); // update the server
            if (_GetGroup(id)?.name == name)
                _GroupNameChanged(id, result.name);
        }

        /// <summary>
        /// Called when we want to update a group's stream_id on snapserver
        /// </summary>
        /// <param name="id">group id</param>
        /// <param name="stream_id">new stream_id</param>
        private async Task _SetGroupStreamAsync(string id, string stream_id)
        {
            Debug("Sending Group.SetStream - id {0}, stream_id {1}", id, stream_id);
            SetStreamCommandResult result = await _InvokeAsync<SetStreamCommandResult>(
                "Group.SetStream",
                new StreamCommand() {id = id, stream_id = stream_id}); // update the server
            if (_GetGroup(id)?.stream_id == stream_id)
                _GroupStreamChanged(id, result.stream_id);
        }

        /// <summary>
        /// Called when we want to update a group's client list on snapserver
        /// </summary>
        /// <param name="id">group id</param>
        /// <param name="clientIds">client ids</param>
        private async Task _SetGroupClientsAsync(string id, string[] clientIds)
        {
            Debug("Sending Group.SetClients - id {0}, clients {1}", id, string.Join(",", clientIds));
            ServerUpdateCommandResult result =
                await _InvokeAsync<ServerUpdateCommandResult>("Group.SetClients",
                    new GroupSetClientsCommand() {id = id, clients = clientIds}); // update the server)
            _ServerUpdated(result.server); // update our local data
        }

        private async Task _SendStreamControlCommandAsync(string stream, string command)
        {
            Debug("Sending Stream.Control - stream {0}, command {1}", stream, command);
            SendStreamControlCommandResult result =
                await _InvokeAsync<SendStreamControlCommandResult>("Stream.Control",
                    new SendStreamControlCommand() {id = stream, command = command});
        }

        private sealed class ClientStatusResult
        {
            public JsonRpcData.Client client { get; set; }
        }

        private sealed class GroupStatusResult
        {
            public Group group { get; set; }
        }

        private sealed class ClientLatencyCommandResult
        {
            public int latency { get; set; }
        }

        private static void Debug(string message, params object[] args)
        {
            System.Diagnostics.Trace.WriteLine(string.Format(message, args), "Snapcast control debug");
        }

        private static void Info(string message, params object[] args)
        {
            System.Diagnostics.Trace.WriteLine(string.Format(message, args), "Snapcast control");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (m_CommandLock)
            {
                if (IsDisposed)
                    return;
                this.Dispose(true);
                m_Lifetime.Cancel();
                // Do not wait on the gate/UI thread: cancellation and closing the
                // socket unblock an active connect/request. Its owner cleans up.
                m_JsonRpc?.Dispose();
                m_Stream?.Dispose();
                m_TcpClient?.Dispose();
                ServerData = null;
            }
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

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
using System;
using System.Collections.Generic;

namespace SnapDotNet.Client.JsonRpcData
{
    /// <summary>
    /// These data classes are a representation of snapserver's Server.GetStatus result
    /// The data throws events when it gets modified by either the server or client
    /// Modifications by the server are handled by the UI (if any exists)
    /// </summary>
    public class Data
    {
        public ServerData server { get; set; }
    }

    public class ServerData
    {
        public Group[] groups { get; set; }
        public Server server { get; set; }
        public Stream[] streams { get; set; }

        /// <summary>
        /// Called when server data is about to get a full refresh
        /// </summary>
        public void SERVER_Invalidate()
        {
            foreach (Group group in groups)
            {
                foreach (Client client in group.clients)
                {
                    client.SERVER_Invalidate();
                }
                group.SERVER_Invalidate();
            }

            foreach (Stream stream in streams)
            {
                stream.SERVER_Invalidate();
            }
        }

        public Client[] GetAllClients()
        {
            List<Client> clients = new List<Client>();
            foreach (Group group in groups)
            {
                clients.AddRange(group.clients);
            }
            return clients.ToArray();
        }
    }

    public class Group
    {
        /// <summary>
        /// OnGroupUpdated event is used to update UI after server reports changes
        /// </summary>
        public event Action SERVER_OnGroupUpdated;

        /// <summary>
        /// OnInvalidate event is used to update UI when server data is about to be fully refreshed
        /// </summary>
        public event Action SERVER_OnInvalidate;

        /// <summary>
        /// OnVolume updated: triggered when the clients update their volumes, caught by UI
        /// </summary>
        public event Action CLIENT_OnVolumeUpdated;

        /// <summary>
        /// OnNameModified: SnapcastClient listens to this for replicating to server
        /// </summary>
        public event Action CLIENT_OnNameModified;

        /// <summary>
        /// OnStreamModified: SnapcastClient listens to this for replicating to server
        /// </summary>
        public event Action CLIENT_OnStreamModified;

        /// <summary>
        /// OnMuteSet: SnapcastClient listens to this for replicating to server
        /// </summary>
        public event Action CLIENT_OnMuteModified;

        /// <summary>
        /// OnClientListModified: SnapcastClient listens to this for replicating to server
        /// </summary>
        public event Action<string[]> CLIENT_OnClientListModified;

        private string m_StreamId;
        private readonly List<int> m_ClientVolumes = new List<int>();
        private int m_GroupVolume = 0;

        private bool m_Muted = false;
        private string m_Name = "";

        public bool muted
        {
            get
            {
                return m_Muted;
            }
            set
            {
                m_Muted = value;
            }
        }

        public string name
        {
            get
            {
                return m_Name;
            }
            set
            {
                m_Name = value;
            }
        }

        public string stream_id
        {
            get
            {
                return m_StreamId;
            }
            set
            {
                m_StreamId = value;
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public string Name
        {
            get
            {
                return string.IsNullOrEmpty(name) == false ? name : "(unnamed)";
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public int VolumePercent
        {
            get
            {
                float total = 0.0f;
                for (int i = 0; i < clients.Length; i++)
                {
                    total += clients[i].config.volume.percent;
                }

                int avg = (int)Math.Ceiling(total / clients.Length);
                return avg;
            }
        }

        public Client[] clients { get; set; }
        public string id { get; set; }

        /// <summary>
        /// Called when the server updates the group's name
        /// </summary>
        /// <param name="name"></param>
        public void SERVER_SetName(string name)
        {
            m_Name = name;
            SERVER_OnGroupUpdated?.Invoke();
        }

        /// <summary>
        /// Called when the server updates the group's mute state
        /// </summary>
        /// <param name="mute"></param>
        public void SERVER_SetMute(bool mute)
        {
            m_Muted = mute;
            SERVER_OnGroupUpdated?.Invoke();
        }

        /// <summary>
        /// Called when we edit a group's volume locally
        /// </summary>
        /// <param name="volume"></param>
        public void CLIENT_SetVolume(int volume)
        {
            if (m_ClientVolumes.Count != clients.Length) // make sure we at least have the same number of clients
            {
                CLIENT_StartGroupVolumeChange();
            }

            float diff = volume - m_GroupVolume;
            if (diff == 0)
                return;

            float ratio;
            if (diff < 0)
                ratio = (m_GroupVolume - volume) / (float)m_GroupVolume;
            else
                ratio = (volume - m_GroupVolume) / (float)((100 - m_GroupVolume));

            for (int i = 0; i < clients.Length; i++)
            {

                float old = m_ClientVolumes[i];
                if (diff < 0)
                {
                    old -= ratio * old;
                }
                else
                {
                    old += ratio * (100 - old);
                }

                clients[i].config.volume.CLIENT_SetPercent((int)old);
            }
        }

        /// <summary>
        /// Called when the server notifies us of a stream change
        /// </summary>
        /// <param name="id"></param>
        public void SERVER_SetStream(string id)
        {
            m_StreamId = id;
            SERVER_OnGroupUpdated?.Invoke();
        }

        /// <summary>
        /// Called when we toggle a group's muted status locally
        /// </summary>
        public void CLIENT_ToggleMuted()
        {
            m_Muted = m_Muted == false;
            CLIENT_OnMuteModified?.Invoke();
        }

        /// <summary>
        /// Called when we edit a group's name locally
        /// </summary>
        /// <param name="name"></param>
        public void CLIENT_SetName(string name)
        {
            if (this.name != name)
            {
                this.name = name;
                CLIENT_OnNameModified?.Invoke();
            }
        }

        /// <summary>
        /// Called when we edit a group's stream locally
        /// </summary>
        /// <param name="stream_id"></param>
        public void CLIENT_SetStream(string stream_id)
        {
            if (this.stream_id != stream_id)
            {
                this.stream_id = stream_id;
                CLIENT_OnStreamModified?.Invoke();
            }
        }

        /// <summary>
        /// Called when we edit a group's clients locally
        /// </summary>
        /// <param name="ids"></param>
        public void CLIENT_SetClients(string[] ids)
        {
            // check if there's any changes
            bool added = false;
            foreach (string id in ids)
            {
                if (HasClientWithId(id) == false)
                {
                    added = true;
                }
            }

            // if a new one's been added, or we have a different # of clients than before,
            // we can guess the collection's been modified :)
            if (added == true || clients.Length != ids.Length)
            {
                CLIENT_OnClientListModified?.Invoke(ids);
            }
        }

        /// <summary>
        /// called when progress bar is pressed
        /// </summary>
        public void CLIENT_StartGroupVolumeChange()
        {
            m_ClientVolumes.Clear();
            for (int i = 0; i < clients.Length; i++)
            {
                m_ClientVolumes.Add(clients[i].config.volume.percent);
            }
            m_GroupVolume = VolumePercent;
        }

        /// <summary>
        /// Called when server data is about to be fully refreshed
        /// </summary>
        public void SERVER_Invalidate()
        {
            SERVER_OnInvalidate?.Invoke();
        }

        /// <summary>
        /// makes the group throw an OnVolumeChanged event when any of the clients update their volumes
        /// </summary>
        public void SubscribeToClientEvents()
        {
            for (int i = 0; i < clients.Length; i++)
            {
                int idx = i;
                clients[i].config.volume.CLIENT_OnModified += () =>
                {
                    CLIENT_OnVolumeUpdated?.Invoke();
                };
            }
        }

        public bool HasClientWithId(string id)
        {
            foreach (Client c in clients)
            {
                if (c.id == id)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class Client
    {
        /// <summary>
        /// OnClientUpdated event is used to update UI after server reports changes
        /// </summary>
        public event Action SERVER_OnClientUpdated;

        /// <summary>
        /// OnNameModified event is used by SnapcastClient for replicating to server
        /// </summary>
        public event Action CLIENT_OnNameModified;

        /// <summary>
        /// OnRemoved event is used by SnapcastClient for replicating to server
        /// </summary>
        public event Action CLIENT_OnRemoved;

        /// <summary>
        /// OnInvalidate event is used to update UI when server data is about to be fully refreshed
        /// </summary>
        public event Action SERVER_OnInvalidate;

        private Lastseen m_LastSeen;
        private bool m_Connected;

        public Config config { get; set; }
        public Host host { get; set; }
        public string id { get; set; }
        public Snapclient snapclient { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public string Name
        {
            get
            {
                if (config != null && string.IsNullOrEmpty(config.name) == false)
                {
                    return config.name;
                }
                return host.name;
            }
            set
            {
                config.name = value;

            }
        }

        public bool connected
        {
            get
            {
                return m_Connected;
            }
            set
            {
                m_Connected = value;
            }
        }

        public Lastseen lastSeen
        {
            get
            {
                return m_LastSeen;
            }
            set
            {
                m_LastSeen = value;
            }
        }

        public void SERVER_SetConnected(bool connected)
        {
            m_Connected = connected;
            SERVER_OnClientUpdated?.Invoke();
        }

        public void SERVER_SetName(string name)
        {
            config.name = name;
            SERVER_OnClientUpdated?.Invoke();
        }

        public void SERVER_SetLastSeen(Lastseen lastSeen)
        {
            m_LastSeen = lastSeen;
            SERVER_OnClientUpdated?.Invoke();
        }

        public void CLIENT_Remove()
        {
            CLIENT_OnRemoved?.Invoke();
        }

        public void SERVER_Invalidate()
        {
            SERVER_OnInvalidate?.Invoke();
        }

        public void CLIENT_SetName(string name)
        {
            if (config.name != name)
            {
                config.name = name;
                CLIENT_OnNameModified?.Invoke();
            }
        }
    }

    public class Config
    {
        /// <summary>
        /// OnVolumeUpdated event is used to update UI after server reports changes
        /// </summary>
        public event Action SERVER_OnVolumeUpdated;

        /// <summary>
        /// OnLatencyModified is used by SnapcastClient to replicate to server
        /// </summary>
        public event Action CLIENT_OnLatencyModified;

        private Volume m_Volume;

        public int instance { get; set; }
        public int latency { get; set; }
        public string name { get; set; }

        public Volume volume
        {
            get
            {
                return m_Volume;
            }
            set
            {
                m_Volume = value;
            }
        }

        public void SERVER_SetVolume(Volume volume)
        {
            m_Volume = volume;
            SERVER_OnVolumeUpdated?.Invoke();
        }

        public void CLIENT_SetLatency(int latency)
        {
            this.latency = latency;
            CLIENT_OnLatencyModified?.Invoke();
        }
    }

    public class Volume
    {
        /// <summary>
        /// OnModified event is subscribed to by SnapcastClient for replicating to server
        /// </summary>
        public event Action CLIENT_OnModified;

        public bool muted { get; set; }

        public int percent { get; set; }

        public void CLIENT_ToggleMuted()
        {
            muted = muted == false;
            CLIENT_OnModified?.Invoke();
        }

        public void CLIENT_SetPercent(int percent)
        {
            if (this.percent != percent)
            {
                this.percent = percent;
                CLIENT_OnModified?.Invoke();
            }
        }

        public override string ToString()
        {
            return string.Format("Percent: {0}, muted: {1}", percent, muted);
        }
    }

    public class Stream
    {
        /// <summary>
        /// OnInvalidate event is used to update UI when server data is about to be fully refreshed
        /// </summary>
        public event Action SERVER_OnInvalidate;

        /// <summary>
        /// OnStreamUpdated event is used to update UI after server reports changes
        /// </summary>
        public event Action SERVER_OnStreamUpdated;

        public string id { get; set; }
        public Meta meta { get; set; }
        public string status { get; set; }
        public Uri uri { get; set; }

        /// <summary>
        /// We use this method to update the stream member my member
        /// The server sends out a new stream object altogether, which would conflict with our event system
        /// </summary>
        /// <param name="stream"></param>
        public void SERVER_Update(Stream stream)
        {
            this.id = stream.id;
            this.meta = stream.meta;
            this.status = stream.status;
            this.uri = stream.uri;
            SERVER_OnStreamUpdated?.Invoke();
        }

        /// <summary>
        /// Called when server data is about to be fully refreshed
        /// </summary>
        public void SERVER_Invalidate()
        {
            SERVER_OnInvalidate?.Invoke();
        }
    }

    public class Server
    {
        public Host host { get; set; }
        public Snapserver snapserver { get; set; }
    }

    public class Host
    {
        public string arch { get; set; }
        public string ip { get; set; }
        public string mac { get; set; }
        public string name { get; set; }
        public string os { get; set; }
    }

    public class Snapserver
    {
        public int controlProtocolVersion { get; set; }
        public string name { get; set; }
        public int protocolVersion { get; set; }
        public string version { get; set; }
    }

    public class Lastseen
    {
        public int sec { get; set; }
        public int usec { get; set; }
    }

    public class Snapclient
    {
        public string name { get; set; }
        public int protocolVersion { get; set; }
        public string version { get; set; }
    }

    public class Meta
    {
        public string STREAM { get; set; }
    }

    public class Uri
    {
        public string fragment { get; set; }
        public string host { get; set; }
        public string path { get; set; }
        public Query query { get; set; }
        public string raw { get; set; }
        public string scheme { get; set; }
    }

    public class Query
    {
        public string chunk_ms { get; set; }
        public string codec { get; set; }
        public string name { get; set; }
        public string sampleformat { get; set; }
    }

    // RPC transaction helper classes

    public class ClientVolumeCommand
    {
        public string id { get; set; }
        public Volume volume { get; set; }
    }

    public class NameCommand
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class StreamCommand
    {
        public string id { get; set; }
        public string stream_id { get; set; }
    }


    public class ClientLatencyCommand
    {
        public string id { get; set; }
        public int latency { get; set; }
    }

    public class GroupSetMutedCommand
    {
        public string id { get; set; }
        public bool mute { get; set; }
    }

    public class GroupSetClientsCommand
    {
        public string id { get; set; }
        public string[] clients { get; set; }
    }

    public class RemoveClientCommand
    {
        public string id { get; set; }
    }

    public class ClientVolumeCommandResult
    {
        public Volume volume { get; set; }
    }

    public class SetMutedCommandResult
    {
        public bool mute { get; set; }
    }

    public class SetNameCommandResult
    {
        public string name { get; set; }
    }

    public class SetStreamCommandResult
    {
        public string stream_id { get; set; }
    }

    public class ServerUpdateCommandResult
    {
        public ServerData server { get; set; }
    }

}

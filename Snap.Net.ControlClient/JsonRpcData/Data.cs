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

namespace SnapDotNet.ControlClient.JsonRpcData
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

        public DateTime GetDateTime()
        {
            System.DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return dateTime.AddSeconds(sec).ToLocalTime();
        }
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

    public class SendStreamControlCommand
    {
        public string id { get; set; }
        public string command { get; set; }
        public Dictionary<string, string> @params {
            get;
            set;
        } = new Dictionary<string, string>();
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

    public class SendStreamControlCommandResult
    {
        public string result { get; set; }
    }
}

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

namespace SnapDotNet.ControlClient.JsonRpcData
{
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
}

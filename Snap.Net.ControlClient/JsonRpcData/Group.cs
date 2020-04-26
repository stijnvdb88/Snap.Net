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
}

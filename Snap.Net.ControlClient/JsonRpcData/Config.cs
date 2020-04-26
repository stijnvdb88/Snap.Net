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
}

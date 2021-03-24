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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using NAudio.CoreAudioApi;
using Snap.Net.Broadcast;

namespace SnapDotNet.Broadcast
{
    /// <summary>
    /// This class is responsible for managing the tcp connection with snapserver over which we send loopback audio
    /// </summary>
    public class Broadcast
    {
        [System.Serializable]
        public class BroadcastSettings
        {
            public string DeviceUniqueId;
            public bool AutoBroadcast;
            public int Port = 4953;
        }

        public struct BroadcastDevice
        {
            public string FriendlyName;
            public string UniqueId;

            public override string ToString()
            {
                return FriendlyName;
            }
        }

        public enum EState
        {
            Broadcasting,
            ActiveButNotConnected,
            Stopped
        }

        public event Action<EState> BroadcastStateChanged = null;
        private EState m_State = EState.Stopped;

        public EState State
        {
            get => m_State;
            private set
            {
                m_State = value; 
                BroadcastStateChanged?.Invoke(value);
            }
        }

        private string m_ActiveDevice = "";
        private BroadcastController m_BroadcastController = null;
        public async Task StartAutoBroadcastAsync()
        {
            BroadcastSettings settings = SnapSettings.GetBroadcastSettings();
            if (settings.AutoBroadcast && string.IsNullOrWhiteSpace(settings.DeviceUniqueId) == false)
            {
                await BroadcastAsync();
            }
        }

        public async Task BroadcastAsync()
        {
            BroadcastSettings settings = SnapSettings.GetBroadcastSettings();
            MMDevice device = Device.GetDevice(settings.DeviceUniqueId);
            if (device != null)
            {
                m_BroadcastController = new BroadcastController(device);
                m_BroadcastController.OnConnected += _OnBroadcastControllerConnected;
                string address = SnapSettings.Server;
                int port = settings.Port;
                await m_BroadcastController.RunAsync(address, port)
                    .ConfigureAwait(false);
            }
        }

        public void Stop()
        {
            m_BroadcastController?.Stop();
            State = EState.Stopped;
            m_BroadcastController = null;
        }

        private void _OnBroadcastControllerConnected(bool connected)
        {
            State = connected ? EState.Broadcasting : EState.ActiveButNotConnected;
        }

        public static List<BroadcastDevice> GetDevices()
        {
            List<MMDevice> wasapiDevices = Device.GetWasapiDevices();
            List<BroadcastDevice> result = new List<BroadcastDevice>();
            foreach (MMDevice wasapiDevice in wasapiDevices)
            {
                result.Add(new BroadcastDevice() { FriendlyName = wasapiDevice.FriendlyName, UniqueId = wasapiDevice.ID});
            }

            return result;
        }
    }
}

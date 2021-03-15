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
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace SnapClient.Net
{
    class WasapiDevice : Device
    {
        public WasapiDevice(Options options) : base(options) { }

        public override IEnumerable<string> List()
        {
            List<MMDevice> devices = _GetWasapiDevices();
            List<string> output = new List<string>();

            for (int i = 0; i < devices.Count; i++)
            {
                output.Add($"{i}: {devices[i].FriendlyName}");
            }

            return output;
        }


        public override IWavePlayer DeviceFactory()
        {
            MMDevice device = _GetDevice(m_Options.SoundCard);
            if (device == null)
            {
                throw new Exception($"Couldn't find Wasapi device with index {m_Options.SoundCard}");
            }
            return new WasapiOut(device, AudioClientShareMode.Shared, true, m_Options.DacLatency);
        }

        private MMDevice _GetDevice(int index)
        {
            List<MMDevice> devices = _GetWasapiDevices();
            if (index < devices.Count)
            {
                return devices[index];
            }
            return null;
        }

        private List<MMDevice> _GetWasapiDevices()
        {
            List<MMDevice> list = new List<MMDevice>();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            list.Add(enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia));

            foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                list.Add(device);
            }

            return list;
        }
    }
}

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

using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace SnapClient.Net
{
    class DirectSoundDevice : Device
    {
        public DirectSoundDevice(Options options) : base(options)
        {
        }

        public override IEnumerable<string> List()
        {
            List<string> output = new List<string>();
            DirectSoundDeviceInfo[] devices = DirectSoundOut.Devices.ToArray();
            for (int i = 0; i < devices.Length; i++)
            {
                output.Add($"{i}: {devices[i].Description}");
            }

            return output;
        }

        private DirectSoundDeviceInfo _GetDevice(int index)
        {
            DirectSoundDeviceInfo[] devices = DirectSoundOut.Devices.ToArray();
            if (index < devices.Length)
            {
                return devices[index];
            }

            return null;
        }

        public override IWavePlayer DeviceFactory()
        {
            DirectSoundDeviceInfo device = _GetDevice(m_Options.SoundCard);
            return new DirectSoundOut(device.Guid, m_Options.DacLatency);
        }
    }
}

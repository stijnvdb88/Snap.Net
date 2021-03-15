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
using NAudio.Wave;

namespace SnapClient.Net
{
    class WaveOutDevice : Device
    {
        public WaveOutDevice(Options options) : base(options)
        {
        }

        public override IEnumerable<string> List()
        {
            List<WaveOutCapabilities> devices = _GetWaveOutDevices();
            List<string> output = new List<string>();

            for (int i = 0; i < devices.Count; i++)
            {
                output.Add($"{i}: {devices[i].ProductName}");
            }

            return output;
        }

        private List<WaveOutCapabilities> _GetWaveOutDevices()
        {
            List<WaveOutCapabilities> list = new List<WaveOutCapabilities>();
            for (int i = 0; i < NAudio.Wave.WaveOut.DeviceCount; i++)
            {
                list.Add(NAudio.Wave.WaveOut.GetCapabilities(i));
            }
            return list;
        }

        public override IWavePlayer DeviceFactory()
        {
            return new WaveOutEvent { DeviceNumber = m_Options.SoundCard, DesiredLatency = m_Options.DacLatency };
        }

        private WaveOutCapabilities _GetDevice(int index)
        {
            return NAudio.Wave.WaveOut.GetCapabilities(index);
        }
    }
}

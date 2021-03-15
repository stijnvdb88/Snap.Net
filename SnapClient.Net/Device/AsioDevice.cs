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
    /// <summary>
    /// Adding it here for completeness, but ASIO is not suitable for command line as we need to release the ASIO driver when we're done,
    /// and we have no reliable way to detect shutdown in a command line application.
    /// could be used as part of a regular application where we get shutdown events and can clean up the AsioOut object properly.
    /// If we don't call Dispose() on the AsioOut device when we're done with it, the device will be locked on subsequent runs.
    /// The only way to release that lock is to reconnect the device, or reboot the PC.
    /// 
    /// You'll also want to make the ASIO control panel available, you can do this via AsioOut.ShowControlPanel();
    /// </summary>
    class AsioDevice : Device
    {
        public AsioDevice(Options options) : base(options)
        {
        }

        public override IEnumerable<string> List()
        {
            return AsioOut.GetDriverNames();
        }

        public override IWavePlayer DeviceFactory()
        {
            return new AsioOut(m_Options.SoundCard);
        }
    }
}

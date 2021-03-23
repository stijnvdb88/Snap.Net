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

namespace SnapDotNet.Player
{
    public enum EShareMode
    {
        Shared,
        Exclusive
    }

    [System.Serializable]
    public struct DeviceSettings
    {
        public string ResampleFormat;
        public EShareMode ShareMode;
        public bool AutoRestartOnFailure;
        public int RestartAttempts;
        public DateTime? LastSeen;
        public bool UseSnapClientNet;
        public string HostId;
    }
}

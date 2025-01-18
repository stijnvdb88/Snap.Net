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

namespace Snap.Net.SnapClient
{
    [System.Serializable]
    public class HelloMessage
    {
        public string MAC { get; set; }
        public string HostName { get; set; }
        public string Version = "0.24.0";
        public string ClientName = "Snap.Net Stream";
        public string OS { get; set; }
        public string Arch { get; set; }
        public int Instance { get; set; }
        public string ID { get; set; }
        public int SnapStreamProtocolVersion { get; set; } = 2;

        public HelloMessage(string macAddress, string os, int instance = 0)
        {
            MAC = macAddress;
            OS = os;
            HostName = Environment.MachineName;
            Arch = "x64";
            Instance = instance;
            ID = Instance == 0 ? MAC : $"{MAC}#{Instance}";
        }

        public HelloMessage(string id, string os, string architecture, string version = "0.30.0")
        {
            MAC = "00:00:00:00:00:00";
            ID = id;
            OS = os;
            Arch = architecture;
            HostName = Environment.MachineName;
            Version = version;
        }
    }
}
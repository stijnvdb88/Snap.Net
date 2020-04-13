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

namespace SnapDotNet
{
    /// <summary>
    /// This class is responsible for all persistent data
    /// </summary>
    public static class SnapSettings
    {
        private const string AUTOLAUNCH_REGISTRY_KEY = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        public static event Action OnThemeChanged;
        /// <summary>
        /// Server ip or hostname
        /// </summary>
        public static string Server
        {
            get
            {
                return Properties.Settings.Default.Server;
            }
            set
            {
                Properties.Settings.Default.Server = value;
                _Save();
            }
        }

        /// <summary>
        /// Server port to connect to for JSON-RPC control
        /// </summary>
        public static int ControlPort
        {
            get
            {
                return Properties.Settings.Default.ControlPort;
            }
            set
            {
                Properties.Settings.Default.ControlPort = value;
                _Save();
            }
        }

        /// <summary>
        /// Server port to connect snapclient to for playback
        /// </summary>
        public static int PlayerPort
        {
            get
            {
                return Properties.Settings.Default.PlayerPort;
            }
            set
            {
                Properties.Settings.Default.PlayerPort = value;
                _Save();
            }
        }

        /// <summary>
        /// Theme to use for views
        /// </summary>
        public static string Theme
        {
            get
            {
                return Properties.Settings.Default.Theme;
            }
            set
            {
                Properties.Settings.Default.Theme = value;
                _Save();
                OnThemeChanged?.Invoke();
            }
        }

        /// <summary>
        /// Accent to use for views
        /// </summary>
        public static string Accent
        {
            get
            {
                return Properties.Settings.Default.Accent;
            }
            set
            {
                Properties.Settings.Default.Accent = value;
                _Save();
                OnThemeChanged?.Invoke();
            }
        }

        public static bool AutoReconnect
        {
            get
            {
                return Properties.Settings.Default.AutoReconnect;
            }
            set
            {
                Properties.Settings.Default.AutoReconnect = value;
                _Save();
            }
        }

        private static void _Save()
        {
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// figure out instance id - we want this id to never change per sound device once we've set it (otherwise snapcast can't know which sound card should have which volume)
        /// prefer the device's index if that instance id is still available, otherwise get the next available one
        /// (the index is subject to change as devices may disappear / reappear at any given time)
        /// </summary>
        /// <param name="deviceUniqueId"></param>
        /// <param name="preferredId"></param>
        /// <returns>instance ID to pass to snapclient</returns>
        public static int DetermineInstanceId(string deviceUniqueId, int preferredId)
        {
            int instanceId = -1;

            Dictionary<int, string> deviceInstanceIds = null;
            string deviceInstanceIdsJson = Properties.Settings.Default.DeviceInstanceIds;
            if (string.IsNullOrEmpty(deviceInstanceIdsJson) == false)
            {
                deviceInstanceIds = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<int, string>>(deviceInstanceIdsJson);
            }
            else
            {
                deviceInstanceIds = new Dictionary<int, string>();
            }

            foreach (KeyValuePair<int, string> kvp in deviceInstanceIds)
            {
                if (kvp.Value == deviceUniqueId)
                {
                    return kvp.Key; // no need to modify settings or anything, bail here
                }
            }

            while (instanceId == -1) // this device hasn't been assigned an instance id yet - check if our preferred one is still free, if not keep going until we find an available one
            {
                if (deviceInstanceIds.ContainsKey(preferredId) == false)
                {
                    instanceId = preferredId; // it was free
                }
                else
                {
                    preferredId++; // check the next one
                }
            }

            // add and save!
            deviceInstanceIds.Add(instanceId, deviceUniqueId);
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(deviceInstanceIds);
            Properties.Settings.Default.DeviceInstanceIds = json;
            _Save();

            return instanceId;
        }

        public static void SetAudioDeviceAutoPlay(string deviceUniqueId, bool autoPlay, string friendlyName)
        {
            Dictionary<string, Tuple<bool, string>> deviceAutoPlayFlags = GetDeviceAutoPlayFlags();

            if (deviceAutoPlayFlags.ContainsKey(deviceUniqueId) == false)
            {
                deviceAutoPlayFlags.Add(deviceUniqueId, new Tuple<bool, string>(autoPlay, friendlyName));
            }
            else
            {
                deviceAutoPlayFlags[deviceUniqueId] = new Tuple<bool, string>(autoPlay, friendlyName);
            }

            Properties.Settings.Default.DeviceAutoPlayFlags = Newtonsoft.Json.JsonConvert.SerializeObject(deviceAutoPlayFlags);
            _Save();
        }

        public static Dictionary<string, Tuple<bool, string>> GetDeviceAutoPlayFlags()
        {
            Dictionary<string, Tuple<bool, string>> deviceAutoPlayFlags = new Dictionary<string, Tuple<bool, string>>();
            string deviceAutoPlayFlagsJson = Properties.Settings.Default.DeviceAutoPlayFlags;
            if (string.IsNullOrEmpty(deviceAutoPlayFlagsJson) == false)
            {
                try
                {
                    deviceAutoPlayFlags = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, Tuple<bool, string>>>(deviceAutoPlayFlagsJson);
                }
                catch
                {
                    return deviceAutoPlayFlags;
                }
            }
            return deviceAutoPlayFlags;
        }

        /// <summary>
        /// Whether our application launches on Windows startup
        /// </summary>
        /// <returns></returns>
        public static bool LaunchesOnWindowsStartup()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(AUTOLAUNCH_REGISTRY_KEY, false);
            return key.GetValue(Snapcast.ProductName) != null;
        }

        public static void ToggleLaunchesOnWindowsStartup(bool launchOnWindowsStartup)
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(AUTOLAUNCH_REGISTRY_KEY, true);
            if (launchOnWindowsStartup == true)
            {
                key.SetValue(Snapcast.ProductName, System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            }
            else
            {
                if (key.GetValue(Snapcast.ProductName) != null)
                {
                    key.DeleteValue(Snapcast.ProductName);
                }
            }
        }

    }
}

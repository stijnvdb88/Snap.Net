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
using SnapDotNet.ControlClient;
using SnapDotNet.ControlClient.JsonRpcData;

namespace SnapDotNet
{
    /// <summary>
    /// This class is responsible for all persistent data
    /// (might move away from Properties.Settings at some point)
    /// </summary>
    public static class SnapSettings
    {
        public enum ENotificationBehaviour
        {
            Default,
            AutoDismiss,
            Disabled
        }

        public enum EDeviceMissingBehaviour
        {
            Default,
            RetrySilent
        }


        private const string AUTOLAUNCH_REGISTRY_KEY = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        public static event Action OnThemeChanged;

        public static void Init()
        {
            //https://stackoverflow.com/questions/534261/how-do-you-keep-user-config-settings-across-different-assembly-versions-in-net/534335#534335
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }
        }

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
                SnapcastClient.AutoReconnect = value;
                _Save();
            }
        }

        public enum ESnapControlDismissMethod
        {
            ClickOutside,
            RightClick
        }

        public static int SnapControlDismiss
        {
            get
            {
                return Properties.Settings.Default.SnapControlDismiss;
            }
            set
            {
                Properties.Settings.Default.SnapControlDismiss = value;
            }
        }

        public static ESnapControlDismissMethod SnapControlDismissMethod
        {
            get { return (ESnapControlDismissMethod) SnapControlDismiss; }
        }


        /// <summary>
        /// Notification behaviour
        /// </summary>
        public static ENotificationBehaviour NotificationBehaviour
        {
            get
            {
                return (ENotificationBehaviour)Properties.Settings.Default.NotificationBehaviour;
            }
            set
            {
                Properties.Settings.Default.NotificationBehaviour = (int)value;
                _Save();
            }
        }

        /// <summary>
        /// Number of seconds after which to auto dismiss any open notification
        /// </summary>
        public static int NotificationAutoDismissSeconds
        {
            get
            {
                return Properties.Settings.Default.NotificationAutoDismissSeconds;
            }
            set
            {
                Properties.Settings.Default.NotificationAutoDismissSeconds = value;
                _Save();
            }
        }

        /// <summary>
        /// Device missing behaviour
        /// </summary>
        public static EDeviceMissingBehaviour DeviceMissingBehaviour
        {
            get
            {
                return (EDeviceMissingBehaviour)Properties.Settings.Default.DeviceMissingBehaviour;
            }
            set
            {
                Properties.Settings.Default.DeviceMissingBehaviour = (int)value;
                _Save();
            }
        }

        /// <summary>
        /// Number of seconds between retries for missing devices
        /// </summary>
        public static int DeviceMissingRetryIntervalSeconds
        {
            get
            {
                return Properties.Settings.Default.DeviceMissingRetryIntervalSeconds;
            }
            set
            {
                Properties.Settings.Default.DeviceMissingRetryIntervalSeconds = value;
                _Save();
            }
        }


        private static void _Save()
        {
            Properties.Settings.Default.Save();
        }

        public static void SaveSetting<T>(string setting, T value)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
            Properties.Settings.Default[setting] = json;
            _Save();
        }

        public static T LoadSetting<T>(string setting) where T : new()
        {
            string json = Properties.Settings.Default[setting].ToString();
            if (json != "")
            {
                try
                {
                    T value = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
                    return value;
                }
                catch
                {
                    return new T();
                }
            }
            return new T();
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

            Dictionary<int, string> deviceInstanceIds = LoadSetting<Dictionary<int, string>>("DeviceInstanceIds");
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
            SaveSetting<Dictionary<int, string>>("DeviceInstanceIds", deviceInstanceIds);
            return instanceId;
        }

        public static Dictionary<string, Tuple<bool, string>> GetDeviceAutoPlayFlags()
        {
            return LoadSetting<Dictionary<string, Tuple<bool, string>>>("DeviceAutoPlayFlags");
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

            SaveSetting<Dictionary<string, Tuple<bool, string>>>("DeviceAutoPlayFlags", deviceAutoPlayFlags);
        }

        public static Player.DeviceSettings GetDeviceSettings(string deviceUniqueId)
        {
            Dictionary<string, Player.DeviceSettings> allSettings = LoadSetting<Dictionary<string, Player.DeviceSettings>>("DeviceSettings");
            if (allSettings.ContainsKey(deviceUniqueId))
            {
                return allSettings[deviceUniqueId];
            }

            return new Player.DeviceSettings();
        }

        public static void SaveDeviceSettings(string deviceId, Player.DeviceSettings settings)
        {
            Dictionary<string, Player.DeviceSettings> allSettings = LoadSetting<Dictionary<string, Player.DeviceSettings>>("DeviceSettings");
            if (allSettings.ContainsKey(deviceId))
            {
                allSettings[deviceId] = settings;
            }
            else
            {
                allSettings.Add(deviceId, settings);
            }
            SaveSetting<Dictionary<string, Player.DeviceSettings>>("DeviceSettings", allSettings);
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

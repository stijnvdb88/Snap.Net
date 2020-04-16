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
using MahApps.Metro.Controls;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace SnapDotNet.Windows
{
    /// <summary>
    /// Interaction logic for Player.xaml
    /// </summary>
    public partial class Player : MetroWindow
    {
        SnapDotNet.Player.Player m_Player;

        private Dictionary<string, SnapDotNet.Controls.Device> m_DeviceControls = new Dictionary<string, Controls.Device>();
        private Dictionary<string, Tuple<bool, string>> m_DeviceAutoPlayFlags = new Dictionary<string, Tuple<bool, string>>();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public Player(SnapDotNet.Player.Player player)
        {
            InitializeComponent();
            m_Player = player;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            m_Player.OnSnapClientErrored += Player_OnSnapClientErrored;// force device refresh if errors happened (to avoid confusing in case device got disconnected)
            m_Player.DevicePlayStateChanged += Player_DevicePlayStateChanged;
            m_DeviceAutoPlayFlags = SnapSettings.GetDeviceAutoPlayFlags();

            _ListDevices();
        }

        private void Player_OnSnapClientErrored()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _ListDevices();
            });
        }

        private void Player_DevicePlayStateChanged(string deviceUniqueId, SnapDotNet.Player.EState state)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (m_DeviceControls.ContainsKey(deviceUniqueId))
                {
                    m_DeviceControls[deviceUniqueId].State = state;
                }
                else
                {
                    Logger.Warn("deviceId {0} not found in m_DeviceControls (did play state change before window was loaded?)", deviceUniqueId);
                }
            });

        }

        private void btRefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            _ListDevices();
        }

        private void _ListDevices()
        {
            m_DeviceControls.Clear();
            spDevices.Children.Clear();
            SnapDotNet.Player.Device[] devices = SnapDotNet.Player.Device.GetDevices();
            foreach (SnapDotNet.Player.Device device in devices)
            {
                Controls.Device d = new Controls.Device(device);
                SnapDotNet.Player.Device dev = device;
                d.State = m_Player.GetState(dev.UniqueId);
                d.OnPlayClicked += () =>
                {
                    if (d.State == SnapDotNet.Player.EState.Stopped)
                    {
                        Task play = Task.Run(() => m_Player.PlayAsync(dev.UniqueId, dev.Name));
                        play.ConfigureAwait(false);
                    }
                    else
                    {
                        m_Player.Stop(dev.UniqueId);
                    }
                };

                d.OnSettingsClicked += () =>
                {
                    DeviceSettings settings = new DeviceSettings(device);
                    settings.ShowDialog();
                };

                d.OnAutoPlayToggled += (bool autoPlay) =>
                {
                    SnapSettings.SetAudioDeviceAutoPlay(dev.UniqueId, autoPlay, dev.Name);
                };

                bool autoPlay = false;
                if (m_DeviceAutoPlayFlags.ContainsKey(dev.UniqueId))
                {
                    autoPlay = m_DeviceAutoPlayFlags[dev.UniqueId].Item1;
                }
                d.SetAutoPlay(autoPlay);
                spDevices.Children.Add(d);
                m_DeviceControls.Add(dev.UniqueId, d);
            }
        }
        private void Mmsys_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo("mmsys.cpl"));
            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

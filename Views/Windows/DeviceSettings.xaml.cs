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
using System;
using System.Windows;
using System.Windows.Input;

namespace SnapDotNet.Windows
{
    /// <summary>
    /// Interaction logic for DeviceSettings.xaml
    /// </summary>
    public partial class DeviceSettings : MetroWindow
    {
        private int[] kSampleRates = new int[] { 44100, 48000, 96000, 192000 };
        private int[] kBitDepths = new int[] { 16, 24 };

        private SnapDotNet.Player.Device m_Device;

        private SnapDotNet.Player.DeviceSettings m_Settings;

        public DeviceSettings(SnapDotNet.Player.Device device)
        {
            InitializeComponent();
            m_Device = device;
            this.Title = device.Name;
            m_Settings = SnapSettings.GetDeviceSettings(device.UniqueId);

            string[] sharemodes = Enum.GetNames(typeof(SnapDotNet.Player.EShareMode));
            foreach (string mode in sharemodes)
            {
                cbSharemode.Items.Add(mode);
            }

            cbSharemode.SelectedIndex = (int)m_Settings.ShareMode;

            cbSampleFormat.Items.Add("(no resample)");
            cbSampleFormat.SelectedIndex = 0;
            int idx = 1;
            foreach (int sr in kSampleRates)
            {
                foreach (int bp in kBitDepths)
                {
                    string sampleFormat = string.Format("{0}:{1}", sr, bp);
                    cbSampleFormat.Items.Add(sampleFormat);
                    if (m_Settings.ResampleFormat == sampleFormat)
                    {
                        cbSampleFormat.SelectedIndex = idx;
                    }
                    idx++;
                }
            }

            tbRestartTries.Text = m_Settings.RestartAttempts.ToString();
            cbAutoRestart.IsChecked = m_Settings.AutoRestartOnFailure;
        }

        private void btSave_Click(object sender, RoutedEventArgs e)
        {
            m_Settings.ResampleFormat = cbSampleFormat.SelectedIndex == 0 ? "" : cbSampleFormat.SelectedItem.ToString();
            m_Settings.ShareMode = (SnapDotNet.Player.EShareMode)cbSharemode.SelectedIndex;
            m_Settings.AutoRestartOnFailure = (bool)cbAutoRestart.IsChecked;
            if (m_Settings.AutoRestartOnFailure)
            {
                m_Settings.RestartAttempts = int.Parse(tbRestartTries.Text, System.Globalization.CultureInfo.CurrentCulture);
            }
            SnapSettings.SaveDeviceSettings(m_Device.UniqueId, m_Settings);
            this.Close();
        }

        private void cbAutoRestart_Toggled(object sender, RoutedEventArgs e)
        {
            tbRestartTries.IsEnabled = (bool)cbAutoRestart.IsChecked;
        }

        private void tbRestartTries_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Utils.IsNumbersOnly(e.Text) == false;
        }
    }
}

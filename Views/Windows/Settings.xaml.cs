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
using MahApps.Metro;
using MahApps.Metro.Controls;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SnapDotNet.Windows
{
    /// <summary>
    /// Interaction logic for Server.xaml
    /// </summary>
    public partial class Settings : MetroWindow
    {
        private AppTheme[] m_AvailableThemes;
        private Accent[] m_AvailableAccents;

        private string[] m_DismissMethods = {"Click outside", "Right-click"};

        private Dictionary<SnapSettings.ENotificationBehaviour, RadioButton> m_NotificationBehaviourRadioButtons = new Dictionary<SnapSettings.ENotificationBehaviour, RadioButton>();
        private Dictionary<SnapSettings.EDeviceMissingBehaviour, RadioButton> m_DeviceMissingBehaviourRadioButtons = new Dictionary<SnapSettings.EDeviceMissingBehaviour, RadioButton>();

        public Settings()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tbHost.Text = SnapSettings.Server;
            tbControlPort.Text = SnapSettings.ControlPort.ToString(CultureInfo.CurrentCulture);
            tbPlayerPort.Text = SnapSettings.PlayerPort.ToString(CultureInfo.CurrentCulture);
            cbAutoLaunch.IsChecked = SnapSettings.LaunchesOnWindowsStartup();
            lbVersion.Content = string.Format("{0}: {1}", Snapcast.ProductName, Snapcast.Version);
            cbAutoReconnect.IsChecked = SnapSettings.AutoReconnect;

            m_NotificationBehaviourRadioButtons.Add(SnapSettings.ENotificationBehaviour.Default, rbDefault);
            m_NotificationBehaviourRadioButtons.Add(SnapSettings.ENotificationBehaviour.AutoDismiss, rbAutoDismiss);
            m_NotificationBehaviourRadioButtons.Add(SnapSettings.ENotificationBehaviour.Disabled, rbDisable);

            m_DeviceMissingBehaviourRadioButtons.Add(SnapSettings.EDeviceMissingBehaviour.Default, rbMissingDeviceError);
            m_DeviceMissingBehaviourRadioButtons.Add(SnapSettings.EDeviceMissingBehaviour.RetrySilent, rbMissingDeviceRetry);

            m_NotificationBehaviourRadioButtons[SnapSettings.NotificationBehaviour].IsChecked = true;
            m_DeviceMissingBehaviourRadioButtons[SnapSettings.DeviceMissingBehaviour].IsChecked = true;

            tbAutoDismissSeconds.Text = SnapSettings.NotificationAutoDismissSeconds.ToString();
            tbAutoDismissSeconds.IsEnabled = (bool)rbAutoDismiss.IsChecked;

            tbMissingDeviceRetrySeconds.Text = SnapSettings.DeviceMissingRetryIntervalSeconds.ToString();
            tbMissingDeviceRetrySeconds.IsEnabled = (bool) rbMissingDeviceRetry.IsChecked;

            tbMissingDeviceExpiryDays.Text = SnapSettings.DeviceMissingExpiryDays.ToString();

            cbHideOfflineClients.IsChecked = SnapSettings.HideOfflineClients;

            foreach (RadioButton rb in m_NotificationBehaviourRadioButtons.Values)
            {
                rb.Checked += rbNotificationBehaviourGroup_CheckChanged;
            }

            foreach (RadioButton rb in m_DeviceMissingBehaviourRadioButtons.Values)
            {
                rb.Checked += rbDeviceMissingBehaviourGroup_CheckChanged;
            }


            System.Tuple<AppTheme, Accent> activeTheme = ThemeManager.DetectAppStyle(Application.Current);

            m_AvailableThemes = ThemeManager.AppThemes.ToArray();
            m_AvailableAccents = ThemeManager.Accents.ToArray();

            for (int i = 0; i < m_AvailableThemes.Length; i++)
            {
                cbTheme.Items.Add(m_AvailableThemes[i].Name);
                if (m_AvailableThemes[i].Name == activeTheme.Item1.Name)
                {
                    cbTheme.SelectedIndex = i;
                }
            }

            for (int i = 0; i < m_AvailableAccents.Length; i++)
            {
                cbAccent.Items.Add(m_AvailableAccents[i].Name);
                if (m_AvailableAccents[i].Name == activeTheme.Item2.Name)
                {
                    cbAccent.SelectedIndex = i;
                }
            }

            foreach (string s in m_DismissMethods)
            {
                cbDismiss.Items.Add(s);
            }

            cbDismiss.SelectedIndex = (int)SnapSettings.SnapControlDismissMethod;
        }

        private void btClose_Click(object sender, RoutedEventArgs e)
        {
            // todo: actually restart socket connection here
            int controlPort = int.Parse(tbControlPort.Text, CultureInfo.CurrentCulture);
            int playerPort = int.Parse(tbPlayerPort.Text, CultureInfo.CurrentCulture);
            bool modified = SnapSettings.Server != tbHost.Text || SnapSettings.ControlPort != controlPort || SnapSettings.PlayerPort != playerPort;
            SnapSettings.Server = tbHost.Text.Trim();
            SnapSettings.ControlPort = controlPort;
            SnapSettings.PlayerPort = playerPort;
            SnapSettings.NotificationAutoDismissSeconds =
                int.Parse(tbAutoDismissSeconds.Text, CultureInfo.CurrentCulture);

            if (int.TryParse(tbMissingDeviceRetrySeconds.Text, NumberStyles.None, CultureInfo.CurrentCulture,
                out var retryInterval))
            {
                SnapSettings.DeviceMissingRetryIntervalSeconds = retryInterval;
            }

            SnapSettings.DeviceMissingExpiryDays =
                int.Parse(tbMissingDeviceExpiryDays.Text, CultureInfo.CurrentCulture);
            
            if (modified == true)
            {
                Snapcast.Instance.Connect();
            }
            Close();
        }

        private void PreviewTextInputNumbersOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Utils.IsNumbersOnly(e.Text) == false;
        }

        private void cbAutoLaunch_Toggled(object sender, RoutedEventArgs e)
        {
            SnapSettings.ToggleLaunchesOnWindowsStartup((bool)cbAutoLaunch.IsChecked);
        }

        private void Theme_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbAccent.SelectedIndex != -1 && cbTheme.SelectedIndex != -1)
            {
                string accent = cbAccent.SelectedItem.ToString();
                string theme = cbTheme.SelectedItem.ToString();
                ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(accent), ThemeManager.GetAppTheme(theme));
                SnapSettings.Theme = theme;
                SnapSettings.Accent = accent;
            }
        }

        private void cbAutoReconnect_Toggled(object sender, RoutedEventArgs e)
        {
            SnapSettings.AutoReconnect = (bool)cbAutoReconnect.IsChecked;
        }

        private void cbDismiss_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SnapSettings.SnapControlDismiss = cbDismiss.SelectedIndex;
        }

        private void rbNotificationBehaviourGroup_CheckChanged(object sender, RoutedEventArgs e)
        {
            tbAutoDismissSeconds.IsEnabled = (bool)rbAutoDismiss.IsChecked;

            foreach (KeyValuePair<SnapSettings.ENotificationBehaviour, RadioButton> kvp in
                m_NotificationBehaviourRadioButtons)
            {
                if (kvp.Value.IsChecked == true)
                {
                    SnapSettings.NotificationBehaviour = kvp.Key;
                }
            }
        }

        private void rbDeviceMissingBehaviourGroup_CheckChanged(object sender, RoutedEventArgs e)
        {
            tbMissingDeviceRetrySeconds.IsEnabled = (bool)rbMissingDeviceRetry.IsChecked;

            foreach (KeyValuePair<SnapSettings.EDeviceMissingBehaviour, RadioButton> kvp in
                m_DeviceMissingBehaviourRadioButtons)
            {
                if (kvp.Value.IsChecked == true)
                {
                    SnapSettings.DeviceMissingBehaviour = kvp.Key;
                }
            }
        }

        private void cbHideOfflineClients_Toggled(object sender, RoutedEventArgs e)
        {
            SnapSettings.HideOfflineClients = (bool) cbHideOfflineClients.IsChecked;
        }
    }
}

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
using MahApps.Metro;
using MahApps.Metro.Controls;
using System.Globalization;
using System.Linq;
using System.Windows;
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
        }

        private void btClose_Click(object sender, RoutedEventArgs e)
        {
            // todo: actually restart socket connection here
            int controlPort = int.Parse(tbControlPort.Text, CultureInfo.CurrentCulture);
            int playerPort = int.Parse(tbPlayerPort.Text, CultureInfo.CurrentCulture);
            bool modified = SnapSettings.Server != tbHost.Text || SnapSettings.ControlPort != controlPort || SnapSettings.PlayerPort != playerPort;
            SnapSettings.Server = tbHost.Text;
            SnapSettings.ControlPort = controlPort;
            SnapSettings.PlayerPort = playerPort;
            if (modified == true)
            {
                Snapcast.Instance.Connect();
            }
            Close();
        }

        private void tbPort_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Utils.IsNumbersOnly(e.Text) == false || e.Text.Length >= 4;
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
    }
}

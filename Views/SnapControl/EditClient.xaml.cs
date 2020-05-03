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

namespace SnapDotNet.SnapControl
{
    // handle what happens when someone deletes the client while we're editing it (close window + show notification?)
    /// <summary>
    /// Interaction logic for EditClient.xaml
    /// </summary>
    public partial class EditClient : MetroWindow
    {
        private readonly SnapDotNet.ControlClient.JsonRpcData.Client m_Client;
        private readonly SnapDotNet.ControlClient.JsonRpcData.Snapserver m_SnapServer;

        public EditClient(SnapDotNet.ControlClient.JsonRpcData.Client client, SnapDotNet.ControlClient.JsonRpcData.Snapserver server)
        {
            InitializeComponent();
            m_Client = client;
            m_SnapServer = server;
            _UpdateData();
            m_Client.SERVER_OnClientUpdated += () =>
            {
                Application.Current.Dispatcher.Invoke(new Action(_UpdateData));
            };

            m_Client.SERVER_OnInvalidate += () =>
            {
                // server is sending us a full data refresh - close this window (this client might be getting deleted for all we know)
                Dispatcher.Invoke(() =>
                {
                    this.Close();
                });
            };
        }

        private void _UpdateData()
        {
            Title = m_Client.Name;
            tbName.Text = m_Client.Name;
            tbLatency.Text = m_Client.config.latency.ToString();
            tbMAC.Text = m_Client.host.mac;
            tbIP.Text = m_Client.host.ip;
            tbID.Text = m_Client.id;
            tbHost.Text = m_Client.host.name;
            tbOS.Text = m_Client.host.os;
            tbArchitecture.Text = m_Client.host.arch;
            tbVersion.Text = m_Client.snapclient.version;
            tbLastSeen.Text = m_Client.connected == true ? "Online" : m_Client.lastSeen.GetDateTime().ToString();
            btRemove.IsEnabled = m_Client.connected == false;

            spWarning.Visibility = m_SnapServer.version == m_Client.snapclient.version ? Visibility.Hidden : Visibility.Visible;
            if (m_SnapServer.version != m_Client.snapclient.version)
            {
                tbWarning.Text = string.Format("Server version is {0}", m_SnapServer.version);
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            m_Client.CLIENT_SetName(tbName.Text);
            m_Client.config.CLIENT_SetLatency(int.Parse(tbLatency.Text, System.Globalization.CultureInfo.CurrentCulture));
            this.Close();
        }

        private void tbLatency_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Utils.IsNumbersOnly(e.Text) == false || e.Text.Length >= 4;
        }

        private void btRemove_Click(object sender, RoutedEventArgs e)
        {
            m_Client.CLIENT_Remove();
            this.Close();
        }
    }
}

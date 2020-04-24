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
using System.Windows;
using System.Windows.Controls;

namespace SnapDotNet.SnapControl
{
    /// <summary>
    /// Interaction logic for Group.xaml
    /// </summary>
    public partial class Group : UserControl
    {
        private readonly SnapDotNet.ControlClient.SnapcastClient m_SnapcastClient = null;
        private List<Client> m_ClientControls = new List<Client>();
        private readonly SnapDotNet.ControlClient.JsonRpcData.Group m_Group;
        private SnapDotNet.ControlClient.JsonRpcData.Stream m_Stream;

        public Group(SnapDotNet.ControlClient.SnapcastClient snapcastClient, SnapDotNet.ControlClient.JsonRpcData.Group group)
        {
            InitializeComponent();
            m_Group = group;
            m_SnapcastClient = snapcastClient;

            foreach (SnapDotNet.ControlClient.JsonRpcData.Client client in m_Group.clients)
            {
                Client c = new Client(client, m_SnapcastClient.ServerData.server.snapserver);
                c.HorizontalAlignment = HorizontalAlignment.Stretch;
                c.HorizontalContentAlignment = HorizontalAlignment.Center;
                m_ClientControls.Add(c);
                spClients.Children.Add(c);
            }

            m_Group.SERVER_OnGroupUpdated += () =>
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(_OnGroupUpdated));
            };

            m_Group.CLIENT_OnVolumeUpdated += () =>
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(_OnGroupUpdated));
            };

            vcGroup.OnMuteToggled += VcGroup_OnMuteToggled;
            vcGroup.OnVolumeChanged += VcGroup_OnVolumeChanged;
            vcGroup.OnVolumeChangeStart += VcGroup_OnVolumeChangeStart;

            _OnGroupUpdated();
        }

        private void VcGroup_OnVolumeChangeStart()
        {
            m_Group.CLIENT_StartGroupVolumeChange();
        }

        private void VcGroup_OnVolumeChanged(int newValue)
        {
            m_Group.CLIENT_SetVolume(newValue);
        }

        private void VcGroup_OnMuteToggled()
        {
            m_Group.CLIENT_ToggleMuted();
        }

        private void _OnGroupUpdated()
        {
            vcGroup.Active = true;
            lbGroupName.Content = m_Group.Name;
            vcGroup.Muted = m_Group.muted;
            vcGroup.OnVolumeChanged -= VcGroup_OnVolumeChanged;
            vcGroup.Percent = m_Group.VolumePercent;
            vcGroup.OnVolumeChanged += VcGroup_OnVolumeChanged;
            if (m_Stream != null)
            {
                m_Stream.SERVER_OnStreamUpdated -= _OnStreamUpdated; // unhook event from old m_Stream object
            }
            m_Stream = m_SnapcastClient.GetStream(m_Group.stream_id);
            m_Stream.SERVER_OnStreamUpdated += _OnStreamUpdated; // hook up event to new m_Stream object
            _OnStreamUpdated();
        }

        private void _OnStreamUpdated()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lbStreamName.Content = m_Stream.id;
                btStream.Style = (Style)FindResource(m_Stream.status == "playing" ? "AccentedSquareButtonStyle" : "SquareButtonStyle");
            });
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue != e.OldValue)
            {
                m_Group.CLIENT_SetVolume(((int)e.NewValue));
            }
        }

        private void btGroup_Click(object sender, RoutedEventArgs e)
        {
            EditGroup editGroup = new EditGroup(m_SnapcastClient, m_Group);
            editGroup.ShowDialog();
        }

        private void btStream_Click(object sender, RoutedEventArgs e)
        {
            ViewStream viewStream = new ViewStream(m_Stream);
            viewStream.ShowDialog();
        }
    }
}

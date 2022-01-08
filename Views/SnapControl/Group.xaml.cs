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
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Stream = SnapDotNet.ControlClient.JsonRpcData.Stream;

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
                if (SnapSettings.HideOfflineClients == false || client.connected == true)
                {
                    Client c = new Client(client, m_SnapcastClient.ServerData.server.snapserver);
                    c.HorizontalAlignment = HorizontalAlignment.Stretch;
                    c.HorizontalContentAlignment = HorizontalAlignment.Center;
                    m_ClientControls.Add(c);
                    spClients.Children.Add(c);
                }
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
                m_Stream.SERVER_OnStreamPropertiesUpdated -= _OnStreamPropertiesUpdated;
            }
            m_Stream = m_SnapcastClient.GetStream(m_Group.stream_id);
            if (m_Stream != null)
            {
                m_Stream.SERVER_OnStreamUpdated += _OnStreamUpdated; // hook up event to new m_Stream object
                _OnStreamUpdated();

                m_Stream.SERVER_OnStreamPropertiesUpdated += _OnStreamPropertiesUpdated;
            }

            _OnStreamPropertiesUpdated();
        }

        private void _OnStreamPropertiesUpdated()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (m_Stream == null || m_Stream.properties.metadata == null)
                {
                    lbStreamNowPlaying.Content = "";
                    imgAlbumArt.Visibility = Visibility.Hidden;
                    spControls.Visibility = Visibility.Hidden;

                    return;
                }
                string title = m_Stream.properties.metadata.title;
                string nowPlaying = "";

                string[] artists = m_Stream.properties.metadata.albumArtist;
                if (artists == null)
                {
                    artists = m_Stream.properties.metadata.artist;
                }

                string artistText = "";
                if (artists != null)
                {
                    artistText = string.Join(", ", artists);
                }

                if (string.IsNullOrEmpty(artistText) == false)
                {
                    nowPlaying = artistText;
                }

                if (string.IsNullOrEmpty(title) == false)
                {
                    if (string.IsNullOrEmpty(nowPlaying) == false)
                    {
                        nowPlaying += $" - {title}";
                    }
                    else
                    {
                        nowPlaying = title;
                    }
                }

                float duration = m_Stream.properties.metadata.duration;
                if (duration > 0 && string.IsNullOrEmpty(nowPlaying) == false)
                {
                    TimeSpan timeSpan = TimeSpan.FromSeconds(duration);
                    nowPlaying += string.Format(" ({0:D2}:{1:D2})", timeSpan.Minutes, timeSpan.Seconds);
                }

                lbStreamNowPlaying.Content = nowPlaying;

                spControls.Visibility = m_Stream.properties.canControl ? Visibility.Visible : Visibility.Hidden;
                Thickness margin = lbStreamNowPlaying.Margin;
                margin.Top = m_Stream.properties.canControl ? 0 : 15;
                lbStreamNowPlaying.Margin = margin;

                lbStreamNowPlaying.FontSize = m_Stream.properties.canControl ? 10 : 12;

                if (string.IsNullOrEmpty(m_Stream.properties.metadata.artUrl) == false)
                {
                    imgAlbumArt.Visibility = Visibility.Visible;
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(m_Stream.properties.metadata.artUrl, UriKind.Absolute);
                    bitmapImage.EndInit();
                    imgAlbumArt.Source = bitmapImage;
                }

                btPlay.IsEnabled = m_Stream.properties.canPlay;
                btPause.IsEnabled = m_Stream.properties.canPause;
                btPrevious.IsEnabled = m_Stream.properties.canGoPrevious;
                btNext.IsEnabled = m_Stream.properties.canGoNext;
            });
        }

        private void _OnStreamUpdated()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                lbStreamName.Content = m_Stream.id;
                btStream.Style = (Style)FindResource(m_Stream.status == "playing" ? "AccentedSquareButtonStyle" : "SquareButtonStyle");
                bool showControls = false;
                if (m_Stream.properties != null)
                {
                    showControls = m_Stream.properties.canControl;
                }
                spControls.Visibility = showControls ? Visibility.Visible : Visibility.Hidden;
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

        private void Group_OnDrop(object sender, DragEventArgs e)
        {
            base.OnDrop(e);
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string dataString = (string) e.Data.GetData(DataFormats.StringFormat);
                ControlClient.JsonRpcData.Group from = null;
                foreach (ControlClient.JsonRpcData.Group group in m_SnapcastClient.ServerData.groups)
                {
                    if (group.HasClientWithId(dataString))
                    {
                        from = group;
                    }
                }

                if (from != m_Group)
                {
                    List<ControlClient.JsonRpcData.Client> clients = new List<ControlClient.JsonRpcData.Client>(from.clients);
                    // remove client from old group
                    clients.Remove(clients.Find(c => c.id == dataString)); 
                    from.CLIENT_SetClients(clients.Select(c => c.id).ToArray());

                    // add to ours
                    clients = new List<ControlClient.JsonRpcData.Client>(m_Group.clients);
                    List<string> ids = clients.Select(c => c.id).ToList();
                    ids.Add(dataString);
                    m_Group.CLIENT_SetClients(ids.ToArray());
                }
            }

        }

        private void BtPlay_OnClick(object sender, RoutedEventArgs e)
        {
            m_Stream.CLIENT_SendControlCommand(Stream.EControlCommand.play);
        }

        private void BtPrevious_OnClick(object sender, RoutedEventArgs e)
        {
            m_Stream.CLIENT_SendControlCommand(Stream.EControlCommand.previous);
        }

        private void BtPause_OnClick(object sender, RoutedEventArgs e)
        {
            m_Stream.CLIENT_SendControlCommand(Stream.EControlCommand.pause);
        }

        private void BtNext_OnClick(object sender, RoutedEventArgs e)
        {
            m_Stream.CLIENT_SendControlCommand(Stream.EControlCommand.next);
        }
    }
}

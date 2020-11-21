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
using MahApps.Metro.IconPacks;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SnapDotNet.SnapControl
{

    /// <summary>
    /// Interaction logic for Client.xaml
    /// </summary>
    public partial class Client : UserControl
    {
        private readonly SnapDotNet.ControlClient.JsonRpcData.Client m_Client = null;
        private readonly SnapDotNet.ControlClient.JsonRpcData.Snapserver m_SnapServer = null;
        private bool m_Dragging = false;
        private const float DRAG_THRESHOLD = 1.0f;
        private Point m_StartPos = new Point(0, 0);

        public Client(SnapDotNet.ControlClient.JsonRpcData.Client client, SnapDotNet.ControlClient.JsonRpcData.Snapserver server)
        {
            InitializeComponent();
            m_Client = client;
            m_SnapServer = server;
            m_Client.config.SERVER_OnVolumeUpdated += () =>
            {
                // execute on ui thread:
                Application.Current.Dispatcher.BeginInvoke(new Action(_OnClientUpdated));
            };

            m_Client.SERVER_OnClientUpdated += () =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(_OnClientUpdated));
            };

            vcClient.OnMuteToggled += VcClient_OnMuteToggled;
            vcClient.OnVolumeChanged += VcClient_OnVolumeChanged;

            _OnClientUpdated();
        }

        private void VcClient_OnVolumeChanged(int newValue)
        {
            m_Client.config.volume.CLIENT_SetPercent(newValue);
        }

        private void VcClient_OnMuteToggled()
        {
            m_Client.config.volume.CLIENT_ToggleMuted();
        }

        private void _OnClientUpdated()
        {
            if (spTitle.Children.Count > 1) // there's a warning icon - remove it
            {
                spTitle.Children.RemoveAt(1);
            }
            if (m_SnapServer.version != m_Client.snapclient.version)
            {
                PackIconBoxIcons icon = new PackIconBoxIcons();
                icon.Width = 15;
                icon.Height = 15;
                // leaving the background brush to null causes mouse over to only trigger when it's over non-transparent portions of the image,
                // which is less than ideal when you're trying to show a tooltip...
                icon.Background = Brushes.Transparent;
                icon.Kind = PackIconBoxIconsKind.RegularError;
                icon.Foreground = Brushes.Orange;
                icon.VerticalAlignment = VerticalAlignment.Center;
                icon.Margin = new Thickness(5, 0, 0, 0);
                icon.ToolTip = string.Format("Warning: snapclient version ({0}) does not match server version ({1})", m_Client.snapclient.version, m_SnapServer.version);
                spTitle.Children.Add(icon);
            }
            tbClient.Text = m_Client.Name;

            vcClient.Muted = m_Client.config.volume.muted;
            vcClient.Active = m_Client.connected;
            vcClient.OnVolumeChanged -= VcClient_OnVolumeChanged; // temporarily unsubscribe from the event so we don't send get a feedback loop here
            vcClient.Percent = m_Client.config.volume.percent;
            vcClient.OnVolumeChanged += VcClient_OnVolumeChanged;

            if (m_Client.connected == false)
            {
                if (tbClient.TextDecorations == null)
                {
                    tbClient.TextDecorations = new TextDecorationCollection();
                }
                tbClient.TextDecorations.Add(TextDecorations.Strikethrough);
            }
            else
            {
                tbClient.TextDecorations = null;
            }
        }

        private void lbClient_Click(object sender, RoutedEventArgs e)
        {
            EditClient editClient = new EditClient(m_Client, m_SnapServer);
            editClient.ShowDialog();
        }

        private void LbClient_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point pos = e.GetPosition(this);
            Vector delta = pos - m_StartPos;
            if ((delta.Length > DRAG_THRESHOLD || m_Dragging) && e.LeftButton == MouseButtonState.Pressed)
            {
                m_Dragging = true;
                DataObject data = new DataObject();
                data.SetData(DataFormats.StringFormat, m_Client.id);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                Debug.WriteLine("DRAGGING");
            }
        }

        private void LbClient_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;
            m_Dragging = false;

            m_StartPos = e.GetPosition(this);
        }

        private void LbClient_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (m_Dragging)
            {
                m_Dragging = false;
                e.Handled = true;
                var button = sender as Button;
                button.ReleaseMouseCapture();
            }
        }
    }
}

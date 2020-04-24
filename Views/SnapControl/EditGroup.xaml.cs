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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SnapDotNet.SnapControl
{
    // handle what happens when someone deletes the group while we're editing it (close window + show notification?)
    /// <summary>
    /// Interaction logic for EditGroup.xaml
    /// </summary>
    public partial class EditGroup : MetroWindow
    {
        private SnapDotNet.ControlClient.JsonRpcData.Client[] m_Clients;

        private readonly SnapDotNet.ControlClient.JsonRpcData.Group m_Group;
        private readonly SnapDotNet.ControlClient.SnapcastClient m_SnapcastClient;

        private CheckBox[] m_ClientCheckBoxes;

        public EditGroup(SnapDotNet.ControlClient.SnapcastClient snapcastClient, SnapDotNet.ControlClient.JsonRpcData.Group group)
        {
            InitializeComponent();
            m_Group = group;
            m_SnapcastClient = snapcastClient;

            m_Group.SERVER_OnInvalidate += () =>
            {
                // server is sending us a full data refresh - close this window (this group might be getting deleted for all we know)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Close();
                });
            };

            m_Group.CLIENT_OnNameModified += Group_Updated;
            m_Group.SERVER_OnGroupUpdated += Group_Updated;

            _UpdateData();
        }

        private void Group_Updated()
        {
            Dispatcher.Invoke(() =>
            {
                _UpdateData();
            });
        }

        private void _UpdateData()
        {
            Title = m_Group.Name;
            tbName.Text = m_Group.name;

            cbStream.Items.Clear();
            foreach (SnapDotNet.ControlClient.JsonRpcData.Stream stream in m_SnapcastClient.ServerData.streams)
            {
                cbStream.Items.Add(stream.id);
            }

            m_Clients = m_SnapcastClient.ServerData.GetAllClients();
            spClients.Children.Clear();
            m_ClientCheckBoxes = new CheckBox[m_Clients.Length];
            for (int i = 0; i < m_ClientCheckBoxes.Length; i++)
            {
                CheckBox cb = new CheckBox();
                cb.Content = m_Clients[i].Name;
                //cb.FlowDirection = FlowDirection.RightToLeft; // (we want the text on the left side of the checkbox...)
                //TextBlock tb = new TextBlock();
                //tb.FlowDirection = FlowDirection.LeftToRight;
                //tb.Text = m_Clients[i].Name;
                //cb.Content = tb;
                cb.Margin = new Thickness(10, 0, 0, 5);
                cb.IsChecked = m_Group.HasClientWithId(m_Clients[i].id);
                m_ClientCheckBoxes[i] = cb;
                spClients.Children.Add(m_ClientCheckBoxes[i]);
            }

            cbStream.SelectedItem = m_Group.stream_id;
        }

        private string[] _GetSelectedClientIds()
        {
            List<string> ids = new List<string>();
            for (int i = 0; i < m_ClientCheckBoxes.Length; i++)
            {
                if (m_ClientCheckBoxes[i].IsChecked == true)
                {
                    ids.Add(m_Clients[i].id);
                }
            }
            return ids.ToArray();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            m_Group.CLIENT_SetName(tbName.Text);
            m_Group.CLIENT_SetStream(cbStream.SelectedItem.ToString());
            m_Group.CLIENT_SetClients(_GetSelectedClientIds());
            this.Close();
        }
    }
}

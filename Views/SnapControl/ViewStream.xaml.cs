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

namespace SnapDotNet.SnapControl
{
    // handle what happens when someone deletes the client while we're editing it (close window + show notification?)
    /// <summary>
    /// Interaction logic for EditClient.xaml
    /// </summary>
    public partial class ViewStream : MetroWindow
    {
        private SnapDotNet.Client.JsonRpcData.Stream m_Stream;

        public ViewStream(SnapDotNet.Client.JsonRpcData.Stream stream)
        {
            InitializeComponent();
            m_Stream = stream;
            _UpdateData();
            m_Stream.SERVER_OnStreamUpdated += () =>
            {
                Application.Current.Dispatcher.Invoke(new Action(_UpdateData));
            };

            m_Stream.SERVER_OnInvalidate += () =>
            {
                // server is sending us a full data refresh - close this window (this client might be getting deleted for all we know)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Close();
                });
            };
        }

        private void _UpdateData()
        {
            Title = m_Stream.id;
            tbName.Text = m_Stream.id;
            tbStatus.Text = m_Stream.status;
            tbMeta.Text = Newtonsoft.Json.JsonConvert.SerializeObject(m_Stream.meta);
            tbScheme.Text = m_Stream.uri.scheme;
            tbPath.Text = m_Stream.uri.path;
            tbChunkMs.Text = m_Stream.uri.query.chunk_ms;
            tbCodec.Text = m_Stream.uri.query.codec;
            tbSampleFormat.Text = m_Stream.uri.query.sampleformat;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}

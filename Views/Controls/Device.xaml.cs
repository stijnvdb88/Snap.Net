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
using System.Windows;
using System.Windows.Controls;

namespace SnapDotNet.Controls
{
    /// <summary>
    /// Interaction logic for Device.xaml
    /// </summary>
    public partial class Device : UserControl
    {
        private Player.EState m_State = Player.EState.Stopped;

        public event Action OnPlayClicked;
        public event Action<bool> OnAutoPlayToggled;
        public event Action OnSettingsClicked;

        public Player.EState State
        {
            get
            {
                return m_State;
            }
            set
            {
                m_State = value;
                imgIcon.Kind = m_State == Player.EState.Playing ? MahApps.Metro.IconPacks.PackIconZondiconsKind.Pause : MahApps.Metro.IconPacks.PackIconZondiconsKind.Play;
                btPlay.IsChecked = m_State == Player.EState.Playing;
            }
        }

        public void SetAutoPlay(bool autoPlay)
        {
            cbAutoPlay.IsChecked = autoPlay;
        }

        private Player.Device m_Device = null;
        public Device(Player.Device device)
        {
            InitializeComponent();
            m_Device = device;
            lbId.Content = m_Device.Index.ToString();
            lbName.Content = m_Device.Name;
        }

        private void btPlay_Click(object sender, RoutedEventArgs e)
        {
            OnPlayClicked?.Invoke();
        }

        private void cbAutoPlay_Toggled(object sender, RoutedEventArgs e)
        {
            OnAutoPlayToggled?.Invoke((bool)cbAutoPlay.IsChecked);
        }

        private void btSettings_Click(object sender, RoutedEventArgs e)
        {
            OnSettingsClicked?.Invoke();
        }
    }
}

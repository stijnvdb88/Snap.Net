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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SnapDotNet.SnapControl
{
    /// <summary>
    /// Interaction logic for VolumeControl.xaml
    /// </summary>
    public partial class VolumeControl : UserControl
    {
        public event Action<int> OnVolumeChanged;
        public event Action OnMuteToggled;
        public event Action OnVolumeChangeStart;
        public event Action OnVolumeChangeEnd;

        private double m_Percent;
        private bool m_Muted;
        private bool m_Active;

        private bool m_UserManipulating = false; // true while user manipulation is ongoing - we ignore incoming progress sets while this is true so we the control doesn't get jittery

        private PackIconBoxIconsKind VolumeLevel
        {
            get
            {
                if (m_Muted)
                {
                    return PackIconBoxIconsKind.RegularVolumeMute;
                }
                if (m_Percent == 0)
                {
                    return PackIconBoxIconsKind.RegularVolume;
                }
                if (m_Percent < 50)
                {
                    return PackIconBoxIconsKind.RegularVolumeLow;
                }

                return PackIconBoxIconsKind.RegularVolumeFull;
            }
        }

        public VolumeControl()
        {
            InitializeComponent();
        }

        public bool Muted
        {
            get
            {
                return m_Muted;
            }
            set
            {
                m_Muted = value;
                _OnDataUpdated();
            }
        }

        public bool Active
        {
            get
            {
                return m_Active;
            }
            set
            {
                m_Active = value;
                _OnDataUpdated();
            }
        }

        public double Percent
        {
            get
            {
                return m_Percent;
            }
            set
            {
                if (m_UserManipulating == false)
                {
                    m_Percent = value;
                    slVolume.Value = m_Percent;
                    _OnDataUpdated();
                }
            }
        }

        private void _OnDataUpdated()
        {
            imgSound.Kind = (MahApps.Metro.IconPacks.PackIconBoxIconsKind)VolumeLevel;
            if (m_Muted)
            {
                imgSound.Opacity = 0.3f;
            }

            slVolume.Opacity = _GetOpacityForState();
            imgSound.Opacity = _GetOpacityForState();
        }

        private float _GetOpacityForState()
        {
            return m_Active ? 0.8f : 0.3f;
        }

        private void slVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue != e.OldValue)
            {
                OnVolumeChanged?.Invoke((int)e.NewValue);
            }
            m_Percent = e.NewValue;
            slVolume.Value = m_Percent;
            _OnDataUpdated();
        }

        private void imgSound_MouseUp(object sender, MouseButtonEventArgs e)
        {
            OnMuteToggled?.Invoke();
        }


        private void imgSound_MouseEnter(object sender, MouseEventArgs e)
        {
            imgSound.Opacity = 1.0f;
        }

        private void imgSound_MouseLeave(object sender, MouseEventArgs e)
        {
            imgSound.Opacity = _GetOpacityForState();
        }


        private void slVolume_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            m_UserManipulating = true;
            OnVolumeChangeStart?.Invoke();
        }

        private void slVolume_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            m_UserManipulating = false;
            OnVolumeChangeEnd?.Invoke();
        }
    }
}

﻿/*
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
using System.Timers;
using System.Windows.Controls;

namespace SnapDotNet.Controls
{
    /// <summary>
    /// Interaction logic for SnapControl.xaml
    /// </summary>
    public partial class Notification : UserControl
    {
        public Action OnClosed;
        private readonly Hardcodet.Wpf.TaskbarNotification.Interop.Point m_Origin;
        private Timer m_Timer = null;

        public Notification(Hardcodet.Wpf.TaskbarNotification.Interop.Point origin, string title, string message)
        {
            InitializeComponent();
            m_Origin = origin;
            tbTitle.Text = title;
            tbMessage.Text = message;

            if (SnapSettings.NotificationBehaviour == SnapSettings.ENotificationBehaviour.AutoDismiss && SnapSettings.NotificationAutoDismissSeconds > 0)
            {
                m_Timer = new Timer(SnapSettings.NotificationAutoDismissSeconds * 1000);
                m_Timer.Start();
                m_Timer.Elapsed += (sender, args) =>
                {
                    Snapcast.TaskbarIcon.CloseBalloon();
                    m_Timer.Stop();
                    m_Timer = null;
                };
            }
        }

        private void _Close()
        {
            if (this.IsMouseOver == true)
            {
                Snapcast.TaskbarIcon.CloseBalloon(); // should check whether current balloon == this somehow
            }
        }

        private void grid_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _Close();
        }
    }
}

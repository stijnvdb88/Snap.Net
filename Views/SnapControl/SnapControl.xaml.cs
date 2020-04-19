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
using System.Windows.Input;

namespace SnapDotNet.SnapControl
{
    /// <summary>
    /// Interaction logic for SnapControl.xaml
    /// </summary>
    public partial class SnapControl : UserControl
    {
        public Action OnClosed;
        private readonly Hardcodet.Wpf.TaskbarNotification.Interop.Point m_Origin;

        private readonly SnapDotNet.Client.SnapcastClient m_SnapcastClient = null;

        public SnapControl(Hardcodet.Wpf.TaskbarNotification.Interop.Point origin, SnapDotNet.Client.SnapcastClient client)
        {
            InitializeComponent();

            Snapcast.HookEvents.MouseDownExt += global_MouseClick;

            m_Origin = origin;
            m_SnapcastClient = client;
            spGroups.Children.Clear();
            m_SnapcastClient.OnServerUpdated += () =>
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(_OnServerUpdated));
            };

            _OnServerUpdated();

            SnapSettings.OnThemeChanged += SnapSettings_OnThemeChanged;
        }



        private void SnapSettings_OnThemeChanged()
        {
            this.UpdateDefaultStyle();
        }

        private void _OnServerUpdated()
        {
            spGroups.Children.Clear();
            foreach (SnapDotNet.Client.JsonRpcData.Group group in m_SnapcastClient.ServerData.groups)
            {
                spGroups.Children.Add(new Group(m_SnapcastClient, group));
            }
        }

        void global_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {

            if (_IsWithinBounds(e.X, e.Y) == false) // close control when user clicks outside of it
            {
                // if the click was inside any of our application's windows, we don't count it as a dismissal
                bool inApp = false;
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.IsMouseOver)
                    {
                        inApp = true;
                    }
                }

                if (inApp == false)
                {
                    _Close();
                }
            }
        }

        private bool _IsWithinBounds(int x, int y)
        {
            return x > m_Origin.X - this.ActualWidth && x <= m_Origin.X && y > m_Origin.Y - this.ActualHeight && y <= m_Origin.Y + 50;
        }

        private void _Close(bool force = false)
        {
            if (this.IsMouseOver == false || force == true)
            {
                Snapcast.HookEvents.MouseDownExt -= global_MouseClick;
                SnapSettings.OnThemeChanged -= SnapSettings_OnThemeChanged;
                Snapcast.TaskbarIcon.CloseBalloon(); // should check whether current balloon == this somehow                
            }
        }

        private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _Close();
        }

    }
}

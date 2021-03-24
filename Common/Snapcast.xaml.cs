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
using Gma.System.MouseKeyHook;
using Hardcodet.Wpf.TaskbarNotification;
using MahApps.Metro;
using NLog;
using SnapDotNet.ControlClient;
using SnapDotNet.Controls;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace SnapDotNet
{
    /// <summary>
    /// Interaction logic for SnapDotNet.xaml
    /// </summary>
    public partial class Snapcast : Application, IDisposable
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public bool IsDisposed { get; private set; }

        public static System.Version Version = null;
        private SnapcastClient m_SnapcastClient = null;

        public Task m_ConnectTask = null;

        public static Snapcast Instance { get; private set; } = null;
        public static TaskbarIcon TaskbarIcon { get; private set; }
        public Player.Player Player { get; private set; }
        public Broadcast.Broadcast Broadcast { get; private set; }

        public static string ProductName { get; private set; }

        public static IKeyboardMouseEvents HookEvents = null;

        [DllImport("Kernel32.dll")]
        private static extern bool AttachConsole(int processId);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _ConfigureLogger();
            AttachConsole(-1); // attach to console if we were launched via command line
            Console.WriteLine(); // send a newline
            SnapSettings.Init();
            SnapcastClient.AutoReconnect = SnapSettings.AutoReconnect;
            HookEvents = Hook.GlobalEvents();

            ThemeManager.ChangeAppStyle(this, ThemeManager.GetAccent(SnapSettings.Accent), ThemeManager.GetAppTheme(SnapSettings.Theme));

            Assembly assembly = Assembly.GetExecutingAssembly();
            Version = assembly.GetName().Version;
            var list = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if (list != null)
            {
                if (list.Length > 0)
                {
                    ProductName = (list[0] as AssemblyProductAttribute).Product;
                }
            }
            Logger.Info("Application started: {0} {1}", ProductName, Version);

            TaskbarIcon = (TaskbarIcon)FindResource("NotifyIcon");
            TaskbarIcon.NoLeftClickDelay = true;
            m_SnapcastClient = new SnapcastClient();

            Instance = this;
            Player = new Player.Player();
            Broadcast = new Broadcast.Broadcast();

            if (string.IsNullOrEmpty(SnapSettings.Server) == false)
            {
                Connect();
            }
            else
            {
                SnapDotNet.Windows.Settings settings = new Windows.Settings();
                settings.ShowDialog();
            }
        }

        public void Activate()
        {

        }

        public void HandleCommandLineParameters(string[] args)
        {

        }

        public bool IsConnected()
        {
            return m_SnapcastClient != null && m_SnapcastClient.ServerData != null;
        }

        public void Connect()
        {
            if (m_ConnectTask != null)
            {
                Logger.Info("Connect task was not null. Status: {0}", m_ConnectTask.Status.ToString());
            }

            Task.Run(_ConnectClientAndStartAutoPlayAsync).ConfigureAwait(false);
        }

        private async Task _ConnectClientAndStartAutoPlayAsync()
        {
            try
            {
                m_ConnectTask = m_SnapcastClient.ConnectAsync(SnapSettings.Server, SnapSettings.ControlPort);
                await m_ConnectTask;
            }
            catch (Exception e)
            {
                await Current.Dispatcher.BeginInvoke(new Action<string, string>(ShowNotification), "Connection error", string.Format("Exception during connect: {0}", e.Message));
            }

            if (m_SnapcastClient.ServerData != null)
            {
                // start auto-play
                _ = Task.Run(Player.StartAutoPlayAsync).ConfigureAwait(false);
                // start auto-broadcast
                _ = Task.Run(Broadcast.StartAutoBroadcastAsync).ConfigureAwait(false);
            }
        }

        public void ShowSnapPane(bool animate = true)
        {
            if (m_ConnectTask == null)
            {
                return;
            }
            if (m_ConnectTask.Status == TaskStatus.Running || m_ConnectTask.Status == TaskStatus.WaitingForActivation)
            {
                // wait for it to complete...
                Task.Run(async () =>
                {
                    while (m_ConnectTask.Status == TaskStatus.Running || m_ConnectTask.Status == TaskStatus.WaitingForActivation)
                    {
                        await Task.Delay(10);
                    }
                    await Current.Dispatcher.BeginInvoke(new Action<bool>(ShowSnapPane), animate);
                }).ConfigureAwait(false);

                return;
            }

            if (m_SnapcastClient.ServerData == null)
            {
                ShowNotification("Connection error", string.Format("Unable to connect to snapserver at {0}:{1}", SnapSettings.Server, SnapSettings.ControlPort));
                return;
            }

            if (TaskbarIcon.CustomBalloon == null || TaskbarIcon.CustomBalloon.IsOpen == false)
            {
                Hardcodet.Wpf.TaskbarNotification.Interop.Point origin = TaskbarIcon.GetPopupTrayPosition();
                origin.Y += 9; // spawn it as close to the taskbar as we can
                TaskbarIcon.CustomPopupPosition = () =>
                {
                    return origin;
                };
                SnapControl.SnapControl sc = new SnapControl.SnapControl(origin, m_SnapcastClient);
                PopupAnimation animation = animate ? PopupAnimation.Slide : PopupAnimation.None;
                TaskbarIcon.ShowCustomBalloon(sc, animation, null);
            }
            else
            {
                TaskbarIcon.CloseBalloon();
            }
        }

        public void ShowNotification(string title, string message)
        {
            if (SnapSettings.NotificationBehaviour == SnapSettings.ENotificationBehaviour.Disabled)
                return;

            Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Logger.Info("Notification: {0}: {1}", title, message);
                Hardcodet.Wpf.TaskbarNotification.Interop.Point origin = TaskbarIcon.GetPopupTrayPosition();
                origin.Y += 9; // spawn it as close to the taskbar as we can
                TaskbarIcon.CustomPopupPosition = () =>
                {
                    return origin;
                };
                Notification notification = new Notification(origin, title, message);
                TaskbarIcon.ShowCustomBalloon(notification, PopupAnimation.Slide, null);
            }));
        }

        private void _ConfigureLogger()
        {
            string prevLogFile = Path.Combine(Utils.GetDefaultDataDirectory(), "SnapDotNet-previous.log");
            string logFile = Path.Combine(Utils.GetDefaultDataDirectory(), "SnapDotNet.log");

            // to avoid getting a huge log file over time, we should create a new one each session. however, the first thing most users do after
            // encountering an error is to restart the app - which would also cause them to lose the logs of that faulty session.
            // to avoid that, we'll preserve the logs for both the current session and the previous one.

            if (File.Exists(prevLogFile)) // on this boot, the previous log file is already 2 sessions old and can be discarded
            {
                File.Delete(prevLogFile);
            }

            if (File.Exists(logFile)) // this is the actual previous log file, rename it
            {
                File.Move(logFile, prevLogFile);
            }

            var config = new NLog.Config.LoggingConfiguration();


            // Start logging to the regular log file:
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = logFile };
            var logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole");

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);

            // Apply config           
            NLog.LogManager.Configuration = config;
        }

        public void Quit()
        {
            HookEvents.Dispose();
            Shutdown();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            m_SnapcastClient.Dispose();
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                this.IsDisposed = true;
            }
        }
    }
}

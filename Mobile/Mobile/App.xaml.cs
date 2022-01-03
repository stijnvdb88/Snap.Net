﻿using System;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using SnapDotNet.Mobile.Views;
using SnapDotNet.ControlClient;
using System.Threading.Tasks;
using SnapDotNet.Mobile.Common;
using SnapDotNet.Mobile.Models;
using SnapDotNet.Mobile.Player;

namespace SnapDotNet.Mobile
{
    public partial class App : Application
    {
        private IPlayer m_Player = null;

        private SnapcastClient m_SnapcastClient = null;
        public static App Instance { get; private set; } = null;
        public Task m_ConnectTask = null;
        public event Action OnPlayStateChanged = null;
        public event Action OnBroadcastStateChanged = null;

        public IPlayer Player
        {
            get { return m_Player; }
        }

        public App()
        {
            InitializeComponent();
            Instance = this;
            m_SnapcastClient = new SnapcastClient();
            SnapcastClient.AutoReconnect = true;
            MainPage = new MainPage(m_SnapcastClient);
        }
        
        public async Task ReconnectAsync()
        {
            Task[] tasks = new Task[2];
            tasks[0] = _ConnectClientAsync();
            tasks[1] = (MainPage as MainPage).NavigateFromMenuAsync((int)MenuItemType.Clients);
            await Task.WhenAll(tasks);
        }

        public void Connect()
        {
            if (m_ConnectTask != null)
            {
                //Logger.Info("Connect task was not null. Status: {0}", m_ConnectTask.Status.ToString());
            }

            Task.Run(_ConnectClientAsync).ConfigureAwait(false);
        }

        private async Task _ConnectClientAsync()
        {
            try
            {
                m_ConnectTask = m_SnapcastClient.ConnectAsync(SnapSettings.Server, SnapSettings.ControlPort);
                await m_ConnectTask;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception during connection: " + e.Message);
            }
        }

        protected override void OnStart()
        {
#if DESPERATE_DEBUGGING_MODE
            SetupExceptionHandlers();
#endif
            Connect();


            m_Player = DependencyService.Get<IPlayer>();
            m_Player.OnPlayStateChanged(() =>
            {
                OnPlayStateChanged?.Invoke();
            });

            m_Player.OnBroadcastStateChanged(() =>
            {
                OnBroadcastStateChanged?.Invoke();
            });
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
            if (m_SnapcastClient.IsConnected() == false)
            {
                Task.Run(_ConnectClientAsync).ConfigureAwait(false);
            }
        }


#if DESPERATE_DEBUGGING_MODE
        private static void SetupExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironmentOnUnhandledExceptionRaiser;
        }

        private static void Log(string msg, string extra)
        {
            Debug.WriteLine(string.Format("{0} - {1}", msg, extra));
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var exception = args.ExceptionObject as Exception;
            if (exception != null)
            {
                Console.WriteLine(exception.GetType());
                App.Log(exception.Message, exception.StackTrace);
            }
            else
            {
                App.Log(args.ExceptionObject.GetType().ToString(),
                    $"{nameof(args.IsTerminating)}: {args.IsTerminating}");
            }
        }

        private static void AndroidEnvironmentOnUnhandledExceptionRaiser(object sender, RaiseThrowableEventArgs e)
        {
            e.Handled = true;
            App.Log(e.Exception.Message, e.Exception.StackTrace);
        }

        private static void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            var aggException = e.Exception;
            var newExc = new Exception("TaskSchedulerOnUnobservedTaskException", aggException);
            
            App.Log(newExc.Message, newExc.StackTrace);
            App.Log(aggException.Message, aggException.StackTrace);
        }
#endif
    }
}

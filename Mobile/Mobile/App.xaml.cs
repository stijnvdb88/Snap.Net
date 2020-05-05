using System;
using System.Diagnostics;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using SnapDotNet.Mobile.Views;
using SnapDotNet.ControlClient;
using System.Threading.Tasks;
using SnapDotNet.Mobile.Common;
using SnapDotNet.Mobile.Models;

namespace SnapDotNet.Mobile
{
    public partial class App : Application
    {
        private SnapcastClient m_SnapcastClient = null;
        public static App Instance { get; private set; } = null;
        public Task m_ConnectTask = null;

        public App()
        {
            InitializeComponent();
            Instance = this;
            m_SnapcastClient = new SnapcastClient();
            SnapcastClient.AutoReconnect = false;
            MainPage = new MainPage(m_SnapcastClient);
        }

        public async Task Reconnect()
        {
            Task[] tasks = new Task[2];
            tasks[0] = _ConnectClientAsync();
            tasks[1] = (MainPage as MainPage).NavigateFromMenu((int)MenuItemType.Clients);
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
                //await Current.Dispatcher.BeginInvoke(new Action<string, string>(ShowNotification), "Connection error", string.Format("Exception during connect: {0}", e.Message));
            }
        }

        protected override void OnStart()
        {
            Connect();
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
    }
}

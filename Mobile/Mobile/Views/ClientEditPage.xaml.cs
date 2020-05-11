using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SnapDotNet.Mobile.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ClientEditPage : ContentPage
    {
        private readonly SnapDotNet.ControlClient.JsonRpcData.Client m_Client;
        private readonly SnapDotNet.ControlClient.JsonRpcData.Snapserver m_SnapServer;

        public ClientEditPage(SnapDotNet.ControlClient.JsonRpcData.Client client, SnapDotNet.ControlClient.JsonRpcData.Snapserver server)
        {
            InitializeComponent();
            m_Client = client;
            m_SnapServer = server;
            _UpdateData();

            m_Client.SERVER_OnClientUpdated += () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _UpdateData();
                });
            };

            m_Client.SERVER_OnInvalidate += () =>
            {
                // server is sending us a full data refresh - close this window (this client might be getting deleted for all we know)
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Navigation.PopAsync().ConfigureAwait(false);
                });
            };
        }

        private void _UpdateData()
        {
            Title = m_Client.Name;
            eName.Text = m_Client.Name;
            eLatency.Text = m_Client.config.latency.ToString();
            lbMac.Text = m_Client.host.mac;
            lbIP.Text = m_Client.host.ip;
            lbId.Text = m_Client.id;
            lbHost.Text = m_Client.host.name;
            lbOS.Text = m_Client.host.os;
            lbArchitecture.Text = m_Client.host.arch;
            lbVersion.Text = m_Client.snapclient.version;
            lbLastSeen.Text = m_Client.connected == true ? "Online" : m_Client.lastSeen.GetDateTime().ToString();
            btRemove.IsEnabled = m_Client.connected == false;

            //spWarning.Visibility = m_SnapServer.version == m_Client.snapclient.version ? Visibility.Hidden : Visibility.Visible;
            //if (m_SnapServer.version != m_Client.snapclient.version)
            //{
            //    tbWarning.Text = string.Format("Server version is {0}", m_SnapServer.version);
            //}
        }

        private void ClientEditPage_OnDisappearing(object sender, EventArgs e)
        {
            m_Client.CLIENT_SetName(eName.Text);
            m_Client.config.CLIENT_SetLatency(int.Parse(eLatency.Text, System.Globalization.CultureInfo.CurrentCulture));
        }

        private void BtRemove_OnClicked(object sender, EventArgs e)
        {
            m_Client.CLIENT_Remove();
            Navigation.PopAsync().ConfigureAwait(false);
        }
    }
}
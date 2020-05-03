using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnapDotNet.Mobile.Views;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SnapDotNet.Mobile.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Client : ContentView
    {
        private readonly SnapDotNet.ControlClient.JsonRpcData.Client m_Client = null;
        private readonly SnapDotNet.ControlClient.JsonRpcData.Snapserver m_SnapServer = null;

        public Client(SnapDotNet.ControlClient.JsonRpcData.Client client, SnapDotNet.ControlClient.JsonRpcData.Snapserver server)
        {
            InitializeComponent();
            m_Client = client;
            m_SnapServer = server;
            m_Client.config.SERVER_OnVolumeUpdated += () =>
            {
                // execute on ui thread:
                MainThread.BeginInvokeOnMainThread(_OnClientUpdated);
            };

            m_Client.SERVER_OnClientUpdated += () =>
            {
                MainThread.BeginInvokeOnMainThread(_OnClientUpdated);
            };

            vcClient.OnMuteToggled += VcClient_OnMuteToggled;
            vcClient.OnVolumeChanged += VcClient_OnVolumeChanged;
            vcClient.OnSettingsTapped += VcClient_OnSettingsTapped;

            _OnClientUpdated();
        }

        private async void VcClient_OnSettingsTapped()
        {
            await Navigation.PushAsync(new ClientEditPage(m_Client, m_SnapServer)).ConfigureAwait(false);
        }

        private void VcClient_OnVolumeChanged(int newValue)
        {
            m_Client.config.volume.CLIENT_SetPercent(newValue);
        }

        private void VcClient_OnMuteToggled()
        {
            m_Client.config.volume.CLIENT_ToggleMuted();
        }

        private void _OnClientUpdated()
        {
            lbClient.Text = m_Client.Name;
            vcClient.Muted = m_Client.config.volume.muted;
            vcClient.Active = m_Client.connected;
            vcClient.OnVolumeChanged -= VcClient_OnVolumeChanged; // temporarily unsubscribe from the event so we don't send get a feedback loop here
            vcClient.Percent = m_Client.config.volume.percent;
            vcClient.OnVolumeChanged += VcClient_OnVolumeChanged;
        }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Mobile.Controls
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

            _OnClientUpdated();
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

            //if (spTitle.Children.Count > 1) // there's a warning icon - remove it
            //{
            //    spTitle.Children.RemoveAt(1);
            //}
            //if (m_SnapServer.version != m_Client.snapclient.version)
            //{
            //    PackIconBoxIcons icon = new PackIconBoxIcons();
            //    icon.Width = 15;
            //    icon.Height = 15;
            //    // leaving the background brush to null causes mouse over to only trigger when it's over non-transparent portions of the image,
            //    // which is less than ideal when you're trying to show a tooltip...
            //    icon.Background = Brushes.Transparent;
            //    icon.Kind = PackIconBoxIconsKind.RegularError;
            //    icon.Foreground = Brushes.Orange;
            //    icon.VerticalAlignment = VerticalAlignment.Center;
            //    icon.Margin = new Thickness(5, 0, 0, 0);
            //    icon.ToolTip = string.Format("Warning: snapclient version ({0}) does not match server version ({1})", m_Client.snapclient.version, m_SnapServer.version);
            //    spTitle.Children.Add(icon);
            //}
            lbClient.Text = m_Client.Name;

            vcClient.Muted = m_Client.config.volume.muted;
            vcClient.Active = m_Client.connected;
            vcClient.OnVolumeChanged -= VcClient_OnVolumeChanged; // temporarily unsubscribe from the event so we don't send get a feedback loop here
            vcClient.Percent = m_Client.config.volume.percent;
            vcClient.OnVolumeChanged += VcClient_OnVolumeChanged;

            //if (m_Client.connected == false)
            //{
            //    lbClient.TextDecorations = TextDecorations.Strikethrough;
            //}
            //else
            //{
            //    lbClient.TextDecorations = TextDecorations.None;
            //}
        }
    }
}
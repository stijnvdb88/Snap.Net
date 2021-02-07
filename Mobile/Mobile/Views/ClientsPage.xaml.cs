using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using SnapDotNet.Mobile.Models;
using SnapDotNet.Mobile.Views;
using SnapDotNet.ControlClient;
using SnapDotNet.ControlClient.JsonRpcData;
using SnapDotNet.Mobile.Common;
using SnapDotNet.Mobile.Player;
using Xamarin.Essentials;
namespace SnapDotNet.Mobile.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ClientsPage : ContentPage
    {
        private SnapcastClient m_Client = null;
        private Label m_ConnectionFailedLabel = null;

        public ClientsPage(SnapcastClient client)
        {
            InitializeComponent();
            m_Client = client;
            m_Client.OnServerUpdated += Client_OnServerUpdated;
            GroupsRefreshView.Command = new AsyncCommand(_ReloadAsync);
        }

        private async Task _ReloadAsync()
        {
            if (m_Client.IsConnected() == false)
            {
                await m_Client.ConnectAsync(SnapSettings.Server, SnapSettings.ControlPort);
            }
            else
            {
                await m_Client.GetServerStatusAsync();
            }
            _Update();
        }

        private void Client_OnServerUpdated()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _Update();
            });
        }

        public ClientsPage()
        {

        }

        void _Update()
        {
            if (MainThread.IsMainThread == false)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _Update();
                });
                return;
            }

            if (App.Instance.Player.SupportsSnapclient() == false)
            {
                if (ToolbarItems.Contains(tbPlay))
                {
                    ToolbarItems.Remove(tbPlay); // remove "Play" button for platforms that don't support it
                }
            }

            tbPlay.IsEnabled = m_Client != null && m_Client.IsConnected() && m_Client.ServerData != null;
            tbPlay.Text = App.Instance.Player.IsPlaying() == false ? "Play" : "Stop";

            _ClearConnectionFailureLabel();

            Groups.Children.Clear();

            if (m_Client != null && m_Client.IsConnected() && m_Client.ServerData != null)
            {
                foreach (Group g in m_Client.ServerData.groups)
                {
                    Controls.Group cGroup = new Controls.Group(m_Client, g);
                    Groups.Children.Add(cGroup);
                }
            }

            try
            {
                GroupsRefreshView.IsRefreshing = m_Client.IsConnected() == false &&
                                                 m_Client?.ConnectionFailed == false;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exc: " + e.Message);
            }

            if (m_Client?.ConnectionFailed == true)
            {
                if (m_ConnectionFailedLabel == null)
                {
                    m_ConnectionFailedLabel = new Label();
                }

                m_ConnectionFailedLabel.HorizontalTextAlignment = TextAlignment.Center;
                m_ConnectionFailedLabel.Margin = new Thickness(40);
                if (string.IsNullOrWhiteSpace(SnapSettings.Server))
                {
                    m_ConnectionFailedLabel.Text ="This app requires Snapserver to be installed and running on your local network.\n" +
                                                     "Fill in the address to your server via\nMenu -> Settings.\n\n" +
                                                     "For more information, visit:\n\nhttps://github.com/badaix/snapcast\nand\nhttps://github.com/stijnvdb88/snap.net";
                }
                else
                {
                    m_ConnectionFailedLabel.Text = $"Couldn't connect to snapserver at {SnapSettings.Server}:{SnapSettings.ControlPort}";
                }
                m_ConnectionFailedLabel.HorizontalOptions = LayoutOptions.CenterAndExpand;
                m_ConnectionFailedLabel.VerticalOptions = LayoutOptions.CenterAndExpand;
                Groups.Children.Add(m_ConnectionFailedLabel);
            }
            else
            {
                _ClearConnectionFailureLabel();
            }
        }

        private void _ClearConnectionFailureLabel()
        {
            if (m_ConnectionFailedLabel != null)
            {
                m_ConnectionFailedLabel.Text = "";
                Groups.Children.Remove(m_ConnectionFailedLabel);
                m_ConnectionFailedLabel = null;
            }
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            _Update();
            App.Instance.OnPlayStateChanged += _Update;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            App.Instance.OnPlayStateChanged -= _Update;
        }

        private void MenuItem_OnClicked(object sender, EventArgs e)
        {
            if (App.Instance.Player.IsPlaying() == false)
            {
                App.Instance.Player.Play(SnapSettings.Server, SnapSettings.PlayerPort);
            }
            else
            {
                App.Instance.Player.Stop();
            }
        }
    }
}
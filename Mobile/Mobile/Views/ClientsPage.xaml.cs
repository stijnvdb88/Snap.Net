using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        private IPlayer m_Player = null;

        public ClientsPage(SnapcastClient client)
        {
            InitializeComponent();
            m_Client = client;
            m_Client.OnServerUpdated += Client_OnServerUpdated;
            m_Player = DependencyService.Get<IPlayer>();
            GroupsRefreshView.Command = new AsyncCommand(_Reload);
        }

        private async Task _Reload()
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
            tbPlay.IsEnabled = m_Player.SupportsSnapclient();

            Groups.Children.Clear();

            if (m_Client != null && m_Client.ServerData != null)
            {
                foreach (Group g in m_Client.ServerData.groups)
                {
                    Controls.Group cGroup = new Controls.Group(m_Client, g);
                    Groups.Children.Add(cGroup);
                }
            }
            GroupsRefreshView.IsRefreshing = (m_Client?.ServerData == null) && m_Client.ConnectionFailed == false;
        }

        //async void OnItemSelected(object sender, EventArgs args)
        //{
        //    var layout = (BindableObject)sender;
        //    var item = (Item)layout.BindingContext;
        //    await Navigation.PushAsync(new ClientDetailPage(new ClientDetailViewModel(item)));
        //}

        //async void AddItem_Clicked(object sender, EventArgs e)
        //{
        //    await Navigation.PushModalAsync(new NavigationPage(new NewItemPage()));
        //}

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _Update();
        }

        private async void MenuItem_OnClicked(object sender, EventArgs e)
        {
            m_Player.PlayAsync("192.168.1.112", 1704);
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using SnapDotNet.Mobile.Models;
using SnapDotNet.ControlClient;

namespace SnapDotNet.Mobile.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : MasterDetailPage
    {
        Dictionary<int, NavigationPage> MenuPages = new Dictionary<int, NavigationPage>();
        private SnapcastClient m_Client;
        public MainPage(SnapcastClient client)
        {
            m_Client = client;
            InitializeComponent();

            MasterBehavior = MasterBehavior.Popover;
            Detail = new NavigationPage(new ClientsPage(m_Client));
            MenuPages.Add((int)MenuItemType.Clients, (NavigationPage)Detail);
            
            //NavPage.RootPage = new ClientsPage(m_Client);
        }

        public async Task NavigateFromMenu(int id)
        {
            if (!MenuPages.ContainsKey(id))
            {
                switch (id)
                {
                    case (int)MenuItemType.Clients:
                        MenuPages.Add(id, new NavigationPage(new ClientsPage(m_Client)));
                        break;
                    case (int)MenuItemType.Settings:
                        MenuPages.Add(id, new NavigationPage(new SettingsPage()));
                        break;
                }
            }

            var newPage = MenuPages[id];

            if (newPage != null && Detail != newPage)
            {
                Detail = newPage;

                if (Device.RuntimePlatform == Device.Android)
                    await Task.Delay(100);

                IsPresented = false;
            }
        }
    }
}
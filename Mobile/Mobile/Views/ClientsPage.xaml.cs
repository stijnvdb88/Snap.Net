using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using SnapDotNet.Mobile.Models;
using SnapDotNet.Mobile.Views;
using SnapDotNet.Mobile.ViewModels;
using SnapDotNet.ControlClient;
using SnapDotNet.ControlClient.JsonRpcData;
using Xamarin.Essentials;
using Debug = System.Diagnostics.Debug;
#if __ANDROID__
using Java.Lang;
using Application = Android.App.Application;
using Process = Java.Lang.Process;
#endif

namespace SnapDotNet.Mobile.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ClientsPage : ContentPage
    {
        private SnapcastClient m_Client = null;

        public ClientsPage(SnapcastClient client)
        {
            InitializeComponent();
            m_Client = client;
            m_Client.OnServerUpdated += Client_OnServerUpdated;
            GroupsRefreshView.Command = new Command(async () =>
                {
                    await m_Client.GetServerStatusAsync();
                }
            );
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
            Groups.Children.Clear();

            if (m_Client != null && m_Client.ServerData != null)
            {
                foreach (Group g in m_Client.ServerData.groups)
                {
                    Controls.Group cGroup = new Controls.Group(m_Client, g);
                    Groups.Children.Add(cGroup);
                }
            }
            GroupsRefreshView.IsRefreshing = m_Client == null || m_Client.ServerData == null;
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
#if __ANDROID__
            var (excitCode, result) = await RunCommand(Path.Combine(Android.App.Application.Context.ApplicationInfo.NativeLibraryDir, "libsnapclient.so"), 
                "-h", "192.168.1.72", "--player", "oboe");
#endif
        }

#if __ANDROID__
        async Task<(int exitCode, string result)> RunCommand(params string[] command)
        {
            string result = null;
            var exitCode = -1;

            try 
            {
                Android.OS.Process.SetThreadPriority(ThreadPriority.Audio);
                var builder = new ProcessBuilder(command);
                var process = builder.Start();
                exitCode = await process.WaitForAsync();

                if (exitCode == 0)
                {
                    using (var inputStreamReader = new StreamReader(process.InputStream))
                    {
                        result = await inputStreamReader.ReadToEndAsync();
                    }
                }
                else if (process.ErrorStream != null)
                {
                    using (var errorStreamReader = new StreamReader(process.ErrorStream))
                    {
                        var error = await errorStreamReader.ReadToEndAsync();
                        result = $"Error {error}";
                    }
                }
            }
            catch (IOException ex)
            {
                result = $"Exception {ex.Message}";
            }

            return (exitCode, result);
        }
#endif
    }
}
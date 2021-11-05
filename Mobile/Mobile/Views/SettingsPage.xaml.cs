﻿using System;
using System.ComponentModel;
using SnapDotNet.Mobile.Common;
using SnapDotNet.Mobile.Models;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SnapDotNet.Mobile.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            eServer.Text = SnapSettings.Server;
            eControlPort.Text = SnapSettings.ControlPort.ToString();
            ePlayerPort.Text = SnapSettings.PlayerPort.ToString();
            eBroadcastPort.Text = SnapSettings.BroadcastPort.ToString();

            slBroadcast.IsVisible = App.Instance.Player.SupportsBroadcast();
        }

        private async void Button_OnClicked(object sender, EventArgs e)
        {
            SnapSettings.Server = eServer.Text.Trim();
            SnapSettings.ControlPort = int.Parse(eControlPort.Text.Trim());
            SnapSettings.PlayerPort = int.Parse(ePlayerPort.Text.Trim());
            SnapSettings.BroadcastPort = int.Parse(eBroadcastPort.Text.Trim());
            await App.Instance.ReconnectAsync().ConfigureAwait(false);
        }
    }
}
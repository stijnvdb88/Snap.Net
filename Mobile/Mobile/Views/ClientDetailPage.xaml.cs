using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using SnapDotNet.Mobile.Models;
using SnapDotNet.Mobile.ViewModels;

namespace SnapDotNet.Mobile.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ClientDetailPage : ContentPage
    {
        ClientDetailViewModel viewModel;

        public ClientDetailPage(ClientDetailViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.viewModel = viewModel;
        }

        public ClientDetailPage()
        {
            InitializeComponent();

            var item = new Item
            {
                Text = "Item 1",
                Description = "This is an item description."
            };

            viewModel = new ClientDetailViewModel(item);
            BindingContext = viewModel;
        }
    }
}
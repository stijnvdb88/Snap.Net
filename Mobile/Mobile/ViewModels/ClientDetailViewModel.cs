using System;

using SnapDotNet.Mobile.Models;

namespace SnapDotNet.Mobile.ViewModels
{
    public class ClientDetailViewModel : BaseViewModel
    {
        public Item Item { get; set; }
        public ClientDetailViewModel(Item item = null)
        {
            Title = item?.Text;
            Item = item;
        }
    }
}

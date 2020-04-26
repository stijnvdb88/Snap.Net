using System;
using System.Collections.Generic;
using System.Text;

namespace SnapDotNet.Mobile.Models
{
    public enum MenuItemType
    {
        Clients,
        Settings
    }
    public class HomeMenuItem
    {
        public MenuItemType Id { get; set; }

        public string Title { get; set; }
    }
}

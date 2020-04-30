using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Foundation;
using SnapDotNet.Mobile.Player;
using UIKit;

[assembly: Dependency(typeof(SnapDotNet.Mobile.iOS.Player.Player))]
namespace SnapDotNet.Mobile.iOS.Player
{
    public class Player : IPlayer
    {
        public bool SupportsSnapclient()
        {
            return false;
        }

        public async void PlayAsync(string host, int port) { }
        public void Stop() {}
    }
}
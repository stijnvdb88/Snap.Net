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
        public Player()
        {

        }

        public void OnPlayStateChanged(Action callback)
        {
            
        }

        public void Init()
        {

        }

        public bool SupportsSnapclient()
        {
            return false;
        }

        public void Play(string host, int port) { }
        public void Stop() {}

        public bool IsPlaying()
        {
            return false;
        }
    }
}
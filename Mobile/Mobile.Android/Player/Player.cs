using System;
using SnapDotNet.Mobile.Player;
using Xamarin.Forms;

[assembly: Dependency(typeof(SnapDotNet.Mobile.Droid.Player.Player))]
namespace SnapDotNet.Mobile.Droid.Player
{
    public class Player : IPlayer
    {

        public Player()
        {

        }

        public void Play(string host, int port)
        {
            MainActivity.Instance.Play(host, port);
        }

        public void Stop()
        {
            MainActivity.Instance.Stop();
        }

        public bool SupportsSnapclient()
        {
            return true;
        }

        public void OnPlayStateChanged(Action callback)
        {
            MainActivity.Instance.OnPlayStateChangedCallback(callback);
        }

        public bool IsPlaying()
        {
            return MainActivity.Instance.IsPlaying();
        }
    }
}
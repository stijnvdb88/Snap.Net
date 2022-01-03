using System;
using SnapDotNet.Mobile.Common;
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

        public void Broadcast(string host, int port, EBroadcastMode broadcastMode)
        {
            MainActivity.Instance.Broadcast(host, port, broadcastMode);
        }

        public void Stop()
        {
            MainActivity.Instance.Stop();
        }

        public void StopBroadcasting()
        {
            MainActivity.Instance.StopBroadcast();
        }


        public bool SupportsSnapclient()
        {
            return true;
        }

        public bool SupportsBroadcast()
        {
            return true;
        }

        public void OnPlayStateChanged(Action callback)
        {
            MainActivity.Instance.OnPlayStateChangedCallback(callback);
        }

        public void OnBroadcastStateChanged(Action callback)
        {
            MainActivity.Instance.OnBroadcastStateChangedCallback(callback);
        }

        public bool IsPlaying()
        {
            return MainActivity.Instance.IsPlaying();
        }

        public bool IsBroadcasting()
        {
            return MainActivity.Instance.IsBroadcasting();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Foundation;
using NAudio.Wave;
using Snap.Net.SnapClient;
using Snap.Net.SnapClient.Player;
using SnapDotNet.Mobile.Player;
using UIKit;

[assembly: Dependency(typeof(SnapDotNet.Mobile.iOS.Player.Player))]
namespace SnapDotNet.Mobile.iOS.Player
{
    public class Player : IPlayer
    {
        private Controller m_Controller = null;
        private Action m_OnPlayStateCallback = null;


        public void OnPlayStateChanged(Action callback)
        {
            m_OnPlayStateCallback = callback;
        }

        public void Init()
        {

        }

        public bool SupportsSnapclient()
        {
            return true;
        }

        public void Play(string host, int port)
        {
            AudioQueuePlayer audioPlayer = new AudioQueuePlayer(80, 20);
            string architecture = ObjCRuntime.Runtime.IsARM64CallingConvention ? "arm64" : "armv7"; // https://github.com/xamarin/xamarin-macios/issues/4907
            m_Controller = new Controller(audioPlayer, new HelloMessage(UIDevice.CurrentDevice.IdentifierForVendor.AsString(),
                $"iOS {UIDevice.CurrentDevice.SystemVersion}", architecture));
            m_Controller.StartAsync(host, port);
            m_OnPlayStateCallback?.Invoke();
        }

        public void Stop()
        {
            m_Controller.Stop();
            m_Controller.Dispose();
            m_Controller = null;
            m_OnPlayStateCallback?.Invoke();
        }

        public bool IsPlaying()
        {
            return m_Controller != null;
        }
    }
}
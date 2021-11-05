using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Foundation;
using MediaPlayer;
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

        private string m_Host;
        private int m_Port;


        public static Player Instance => s_Instance;
        private static Player s_Instance = null;

        private MPRemoteCommandCenter m_CommandCenter = null;

        public void OnPlayStateChanged(Action callback)
        {
            m_OnPlayStateCallback = callback;
        }

        public Player()
        {
            s_Instance = this;
            m_CommandCenter = MPRemoteCommandCenter.Shared;
            m_CommandCenter.PauseCommand.Enabled = true;
            m_CommandCenter.PauseCommand.AddTarget(_Pause);
            m_CommandCenter.PlayCommand.Enabled = true;
            m_CommandCenter.PlayCommand.AddTarget(_Play);
        }

        private MPRemoteCommandHandlerStatus _Pause(MPRemoteCommandEvent arg)
        {
            Stop();
            return MPRemoteCommandHandlerStatus.Success;
        }

        private MPRemoteCommandHandlerStatus _Play(MPRemoteCommandEvent arg)
        {
            Play();
            return MPRemoteCommandHandlerStatus.Success;
        }


        public bool SupportsSnapclient()
        {
            return true;
        }


        public void Play()
        {
            AudioQueuePlayer audioPlayer = new AudioQueuePlayer(80, 20);
            string architecture = ObjCRuntime.Runtime.IsARM64CallingConvention ? "arm64" : "armv7"; // https://github.com/xamarin/xamarin-macios/issues/4907
            m_Controller = new Controller(audioPlayer, new HelloMessage(UIDevice.CurrentDevice.IdentifierForVendor.AsString(),
                $"iOS {UIDevice.CurrentDevice.SystemVersion}", architecture));
            m_Controller.StartAsync(m_Host, m_Port);
            m_OnPlayStateCallback?.Invoke();
            _UpdateInfoCenter();
        }

        private void _UpdateInfoCenter()
        {
            // todo: instead of hardcoded title, show currently playing stream name?
            MPNowPlayingInfoCenter.DefaultCenter.NowPlaying = IsPlaying() ? new MPNowPlayingInfo { PlaybackRate = 1.0f, Title = "Snapcast" } : new MPNowPlayingInfo { PlaybackRate = 0.0f };
        }

        public void Play(string host, int port)
        {
            m_Host = host;
            m_Port = port;
            Play();
        }

        public void Stop()
        {
            m_Controller.Stop();
            m_Controller.Dispose();
            m_Controller = null;
            m_OnPlayStateCallback?.Invoke();
            _UpdateInfoCenter();
        }

        public bool IsPlaying()
        {
            return m_Controller != null;
        }



        public bool SupportsBroadcast()
        {
            return false;
        }

        public void OnBroadcastStateChanged(Action callback)
        {
        }

        public void Broadcast(string host, int port)
        {
        }

        public void StopBroadcasting()
        {
        }

        public bool IsBroadcasting()
        {
            return false;
        }
    }
}
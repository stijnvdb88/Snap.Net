using System;
using System.Collections.Generic;
using System.Text;

namespace SnapDotNet.Mobile.Player
{
    public interface IPlayer
    {
        void OnPlayStateChanged(Action callback);
        bool SupportsSnapclient();
        void Play(string host, int port);
        void Stop();
        bool IsPlaying();

        void OnBroadcastStateChanged(Action callback);
        bool SupportsBroadcast();
        void Broadcast(string host, int port);
        void StopBroadcasting();
        bool IsBroadcasting();
    }
}

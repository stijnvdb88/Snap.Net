using System;
using System.Collections.Generic;
using System.Text;

namespace SnapDotNet.Mobile.Player
{
    public interface IPlayer
    {
        void Init();
        bool SupportsSnapclient();
        void PlayAsync(string host, int port);
        void Stop();
        bool IsPlaying();
    }
}

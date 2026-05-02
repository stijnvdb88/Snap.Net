using System;

namespace Snap.Net.Broadcast
{
    public interface IAudioDevice
    {
        string FriendlyName { get; }
        
        event EventHandler<byte[]> OnPcm16DataAvailable;
        void Start();
        void Stop();
    }
}
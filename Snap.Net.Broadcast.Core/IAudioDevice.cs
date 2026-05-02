using System;

namespace Snap.Net.Broadcast
{
    public interface IAudioDevice
    {
        string Id { get; }
        string FriendlyName { get; }
        
        event EventHandler<byte[]> OnPcm16DataAvailable;
        void Start();
        void Stop();
    }
}
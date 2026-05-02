using System;

namespace Snap.Net.Broadcast
{
    public interface IAudioDevice
    {
        ISampleFormat SampleFormat { get; }
        
        string FriendlyName { get; }
        
        event EventHandler<byte[]> OnPcm16DataAvailable;
        void Start();
        void Stop();
    }
}
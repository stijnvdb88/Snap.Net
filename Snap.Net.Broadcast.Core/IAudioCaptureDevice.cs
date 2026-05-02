namespace Snap.Net.Broadcast
{
    public interface IAudioCaptureDevice
    {
        bool IsLoopback { get; }
        string DeviceName { get;  }
    }
}
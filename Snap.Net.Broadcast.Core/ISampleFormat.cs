namespace Snap.Net.Broadcast
{
    public interface ISampleFormat
    {
        int Channels { get; }
        int Rate { get; }
        int Bits { get; }
    }
}
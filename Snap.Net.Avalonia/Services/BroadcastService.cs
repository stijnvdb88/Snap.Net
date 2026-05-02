using System.Collections.Generic;
using Snap.Net.Avalonia.Broadcast;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Broadcast;

namespace Snap.Net.Avalonia.Services;

public class BroadcastService : IBroadcastService
{
    public IEnumerable<IAudioDevice> GetAudioDevices(EDeviceType deviceType)
    {
        return Device.GetDevices(deviceType);
    }
}
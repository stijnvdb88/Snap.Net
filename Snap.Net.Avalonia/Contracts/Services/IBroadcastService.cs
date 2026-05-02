using System.Collections.Generic;
using Snap.Net.Avalonia.Broadcast;
using Snap.Net.Broadcast;
using System.Collections.Generic;

namespace Snap.Net.Avalonia.Contracts.Services;

public interface IBroadcastService
{
    
    IEnumerable<IAudioDevice> GetAudioDevices(EDeviceType deviceType);
}
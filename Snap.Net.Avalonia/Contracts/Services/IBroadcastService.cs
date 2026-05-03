using System.Collections.Generic;
using Snap.Net.Avalonia.Broadcast;
using Snap.Net.Broadcast;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Snap.Net.Avalonia.Contracts.Services;

public interface IBroadcastService
{
    IEnumerable<IAudioDevice> GetAudioDevices(EDeviceType deviceType);

    IAudioDevice? GetAudioDevice(string id);
    event Action<bool> OnIsBroadcastingChanged;
    event Action<bool> OnIsConnectedChanged;
    Task StartBroadcast(string host, int port, string deviceId);
    
    void StopBroadcast();
    
    bool IsConnected { get; }
}
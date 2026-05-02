using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Snap.Net.Avalonia.Broadcast;
using Snap.Net.Avalonia.Contracts.Services;
using Snap.Net.Broadcast;

namespace Snap.Net.Avalonia.Services;

public class BroadcastService : IBroadcastService
{
    private BroadcastController? m_BroadcastController;
    private CancellationTokenSource? m_BroadcastCancellationTokenSource;
    private Task? m_BroadcastTask;
    
    public event Action<bool> OnIsBroadcastingChanged = null;
    public event Action<bool> OnIsConnectedChanged = null;
    
    public IAudioDevice? GetAudioDevice(string id)
    {
        return Device.GetDevice(id);
    }
    public IEnumerable<IAudioDevice> GetAudioDevices(EDeviceType deviceType)
    {
        return Device.GetDevices(deviceType);
    }

    public async Task StartBroadcast(string host, int port, IAudioDevice audioDevice)
    {
        await _StopBroadcast();
        m_BroadcastCancellationTokenSource = new CancellationTokenSource();
        m_BroadcastTask = _RunBroadcast(host, port, audioDevice);
        _ = m_BroadcastTask.ContinueWith(t =>
        {
            OnIsConnectedChanged?.Invoke(false);
        }, TaskScheduler.Default);
    }

    private async Task _RunBroadcast(string host, int port, IAudioDevice audioDevice)
    {
        try
        {
            OnIsConnectedChanged?.Invoke(true);
            m_BroadcastController = new BroadcastController(audioDevice);
            m_BroadcastController.OnCapturingAudio += _OnCapturingAudio;
            await m_BroadcastController.RunAsync(host, port);
        }
        finally
        {
            OnIsConnectedChanged?.Invoke(false);
        }
    }

    private async Task _StopBroadcast()
    {
        if (m_BroadcastCancellationTokenSource != null)
        {
            await m_BroadcastCancellationTokenSource.CancelAsync();
            m_BroadcastCancellationTokenSource.Dispose();
            m_BroadcastCancellationTokenSource = null;
        }

        if (m_BroadcastTask != null)
        {
            try { await m_BroadcastTask; }
            catch (OperationCanceledException) { } // expected
            m_BroadcastTask = null;
        }
    } 
    
    private void _OnCapturingAudio(bool capturing, string deviceName)
    {
        OnIsBroadcastingChanged?.Invoke(capturing);
    }

    public void StopBroadcast()
    {
        m_BroadcastController?.Stop();
        m_BroadcastController = null;
    }
}
using System.Diagnostics;
using System.Runtime.Versioning;
using Snap.Net.Broadcast;

namespace Snap.Net.Avalonia.Broadcast;

[SupportedOSPlatform("linux")]
public class LinuxDevice : IAudioDevice
{
    private readonly string m_SourceName;
    private readonly string m_FriendlyName;
    private Process? m_Process;
    private CancellationTokenSource? m_Cts;

    public string Id => m_SourceName;
    public string FriendlyName => m_FriendlyName;
    public event EventHandler<byte[]>? OnPcm16DataAvailable;
    
    public void Start()
    {
        throw new NotImplementedException();
    }

    public void Stop()
    {
        throw new NotImplementedException();
    }

    public LinuxDevice(string sourceName, string friendlyName)
    {
        m_SourceName = sourceName;
        m_FriendlyName = friendlyName;
    }
    public static List<IAudioDevice> GetDevicesAsync(EDeviceType deviceType = EDeviceType.Output)
    {
        List<IAudioDevice> devices = new List<IAudioDevice>();
        List<PulseAudioSource> sources = PulseAudioDevices.Enumerate();

        foreach (PulseAudioSource source in sources)
        {
            bool isMonitor = source.IsMonitor;
            bool include;
            switch (deviceType)
            {
                case EDeviceType.Output:
                    include = isMonitor;
                    break;
                case EDeviceType.Input:
                    include = !isMonitor;
                    break;
                case EDeviceType.All:
                    include = true;
                    break;
                default:
                    include = false;
                    break;
            }

            if (include)
                devices.Add(new LinuxDevice(source.Name, source.Description));
        }

        return devices;
    }
}
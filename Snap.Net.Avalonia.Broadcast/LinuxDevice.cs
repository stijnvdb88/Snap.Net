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
    
    
    public LinuxDevice(string sourceName, string friendlyName)
    {
        m_SourceName = sourceName;
        m_FriendlyName = friendlyName;
    }
    
    public void Start()
    {
        m_Cts = new CancellationTokenSource();
        m_Process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "parec",
                Arguments = $"--device={m_SourceName} --format=s16le --rate=48000 --channels=2 --latency-msec=10",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        m_Process.Start();

        // read parec stdout:
        _ = _ReadAudioAsync(m_Process.StandardOutput.BaseStream, m_Cts.Token);
    }

    public void Stop()
    {
        m_Cts?.Cancel();
        try
        {
            if (m_Process != null && !m_Process.HasExited)
            {
                m_Process.Kill();
                m_Process.WaitForExit(2000);
            }
        }
        catch { /* process may already be gone */ }
        finally
        {
            m_Process?.Dispose();
            m_Process = null;
        }
    }
    
    private async Task _ReadAudioAsync(Stream stdout, CancellationToken ct)
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                int read = await stdout.ReadAsync(buffer, 0, buffer.Length, ct);
                if (read == 0) break; 

                byte[] data = new byte[read];
                Buffer.BlockCopy(buffer, 0, data, 0, read);
                OnPcm16DataAvailable?.Invoke(this, data);
            }
        }
        catch (OperationCanceledException)
        {
        }
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
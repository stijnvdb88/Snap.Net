using System.Diagnostics;
using System.Runtime.Versioning;

namespace Snap.Net.Avalonia.Broadcast;

[SupportedOSPlatform("linux")]
public class PulseAudioSource
{
    public string Name { get; init; }       
    public string Description { get; init; } 
    public bool IsMonitor { get; init; }
}

[SupportedOSPlatform("linux")]
public static class PulseAudioDevices
{
    public static List<PulseAudioSource> Enumerate()
    {
        List<PulseAudioSource> result = new List<PulseAudioSource>();

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "pactl",
            Arguments = "list sources",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using Process proc = Process.Start(psi)!;
        string output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        string[] blocks = output.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        foreach (string block in blocks)
        {
            string? nameLine = block.Split('\n')
                .FirstOrDefault(l => l.TrimStart().StartsWith("Name:"));
            string? descLine = block.Split('\n')
                .FirstOrDefault(l => l.TrimStart().StartsWith("Description:"));

            if (nameLine == null || descLine == null) continue;

            string name = nameLine.Split(':', 2)[1].Trim();
            string desc = descLine.Split(':', 2)[1].Trim();

            result.Add(new PulseAudioSource
            {
                Name = name,
                Description = desc,
                IsMonitor = name.EndsWith(".monitor")
            });
        }

        return result;
    }
}
using Snap.Net.Broadcast;

namespace Snap.Net.Avalonia.Broadcast;

public class Device
{
    public static List<IAudioDevice> GetDevices(EDeviceType deviceType = EDeviceType.Output)
    {
        if (OperatingSystem.IsWindows())
        {
            return WindowsDevice.GetWasapiDevices(deviceType);
        }

        return new List<IAudioDevice>();
    }
    
    public static IAudioDevice? GetDevice(int index, EDeviceType deviceType = EDeviceType.Output)
    {
        List<IAudioDevice> devices = GetDevices(deviceType);
        if (index < devices.Count)
        {
            return devices[index];
        }
        return null;
    }
    
    public static IAudioDevice? GetDevice(string id)
    {
        List<IAudioDevice> devices = GetDevices();
        for (int i = 0; i < devices.Count; i++)
        {
            if (devices[i].Id == id)
            {
                return devices[i];
            }
        }
        return null;
    }
}
using Snap.Net.Broadcast;

namespace Snap.Net.Avalonia.Broadcast;

public class Device
{
    public static IEnumerable<IAudioDevice> GetDevices(EDeviceType deviceType = EDeviceType.All)
    {
        if (OperatingSystem.IsWindows())
        {
            return WindowsDevice.GetWasapiDevices(deviceType);
        }

        return new List<IAudioDevice>();
    }
    
    public static IAudioDevice? GetDevice(int index, EDeviceType deviceType = EDeviceType.All)
    {
        List<IAudioDevice> devices = GetDevices(deviceType).ToList();
        if (index < devices.Count)
        {
            return devices[index];
        }
        return null;
    }
    
    public static IAudioDevice? GetDevice(string id)
    {
        IEnumerable<IAudioDevice> devices = GetDevices();
        return devices.SingleOrDefault(x => x.Id == id);
    }
}
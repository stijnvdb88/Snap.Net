using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;

namespace Snap.Net.Broadcast
{
    public class Device
    {
        public static MMDevice GetDevice(int index)
        {
            List<MMDevice> devices = GetWasapiDevices();
            if (index < devices.Count)
            {
                return devices[index];
            }
            return null;
        }

        public static List<MMDevice> GetWasapiDevices()
        {
            List<MMDevice> list = new List<MMDevice>();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            list.Add(enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia));

            foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                list.Add(device);
            }

            return list;
        }

        public static void PrintDevices()
        {
            List<MMDevice> deviceList = GetWasapiDevices();
            for (int i = 0; i < deviceList.Count; i++)
            {
                Console.WriteLine($"{i}: {deviceList[i].FriendlyName}");
            }
        }
    }
}

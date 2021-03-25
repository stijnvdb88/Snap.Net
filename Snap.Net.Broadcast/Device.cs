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
        public static MMDevice GetDevice(int index, DataFlow dataFlow = DataFlow.Render)
        {
            List<MMDevice> devices = GetWasapiDevices(dataFlow);
            if (index < devices.Count)
            {
                return devices[index];
            }
            return null;
        }

        public static MMDevice GetDevice(string id)
        {
            List<MMDevice> devices = GetWasapiDevices();
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i].ID == id)
                {
                    return devices[i];
                }
            }
            return null;
        }

        public static List<MMDevice> GetWasapiDevices(DataFlow dataFlow = DataFlow.Render)
        {
            List<MMDevice> list = new List<MMDevice>();
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            list.Add(enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia));

            foreach (MMDevice device in enumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active))
            {
                list.Add(device);
            }

            return list;
        }


        public static void PrintDevices(DataFlow dataFlow = DataFlow.Render)
        {
            List<MMDevice> deviceList = GetWasapiDevices(dataFlow);
            for (int i = 0; i < deviceList.Count; i++)
            {
                Console.WriteLine($"{i}: {deviceList[i].FriendlyName}");
            }
        }
    }
}

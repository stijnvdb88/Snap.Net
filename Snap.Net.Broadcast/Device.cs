using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Snap.Net.Broadcast
{
    public class Device : IAudioDevice
    {
        private MMDevice m_Device;
        private WasapiCapture m_Capture = null;
        
        public string Id => m_Device.ID;
        public string FriendlyName => m_Device.FriendlyName;
        public event EventHandler<byte[]> OnPcm16DataAvailable;
        
        public Device(MMDevice device)
        {
            m_Device = device;
            m_Capture = m_Device.DataFlow == DataFlow.Render ? new WasapiLoopbackCapture(m_Device) : new WasapiCapture(m_Device);
            m_Capture.DataAvailable += _OnDataAvailable;
        }

        private void _OnDataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] data = _ToPcm16(e.Buffer, e.BytesRecorded, m_Capture.WaveFormat);
            OnPcm16DataAvailable?.Invoke(this, data);
        }

        /// <summary>
        /// Converts an IEEE Floating Point audio buffer into a 16bit PCM compatible buffer.
        /// </summary>
        /// <param name="inputBuffer">The buffer in IEEE Floating Point format.</param>
        /// <param name="length">The number of bytes in the buffer.</param>
        /// <param name="format">The WaveFormat of the buffer.</param>
        /// <returns>A byte array that represents the given buffer converted into PCM format.</returns>
        /// source: https://stackoverflow.com/questions/65467635/converting-wasapiloopbackcapture-buffer-to-pcm
        private static byte[] _ToPcm16(byte[] inputBuffer, int length, WaveFormat format)
        {
            if (length == 0)
                return new byte[0]; // No bytes recorded, return empty array.

            // Create a WaveStream from the input buffer.
            using (MemoryStream memStream = new MemoryStream(inputBuffer, 0, length))
            {
                // Convert the input stream to a WaveProvider in 16bit PCM format with sample rate of 48000 Hz.
                using (RawSourceWaveStream inputStream = new RawSourceWaveStream(memStream, format))
                {
                    SampleToWaveProvider16 convertedPCM = new SampleToWaveProvider16(
                        new WdlResamplingSampleProvider(
                            new WaveToSampleProvider(inputStream),
                            96000 / format.Channels)
                    );

                    byte[] convertedBuffer = new byte[length];

                    using (MemoryStream stream = new MemoryStream())
                    {
                        int read;

                        // Read the converted WaveProvider into a buffer and turn it into a Stream.
                        while ((read = convertedPCM.Read(convertedBuffer, 0, length)) > 0)
                            stream.Write(convertedBuffer, 0, read);

                        // Return the converted Stream as a byte array.
                        return stream.ToArray();
                    }
                }
            }
        }
        
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

        
        public void Start()
        {
            m_Capture.StartRecording();
        }

        public void Stop()
        {
            m_Capture?.StopRecording();
            m_Capture?.Dispose();
        }
    }
}

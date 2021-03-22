using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Snap.Net.Broadcast
{
    class Program
    {
        private static ClientConnection s_Connection;
        private static WasapiLoopbackCapture s_Capture = null;

        static int Main(string[] args)
        {
            Task<int> result = Parser.Default.ParseArguments<Options>(args)
                .MapResult(_RunAsync, _HandleParseErrorAsync);

            return result.Result;
        }


        static async Task<int> _RunAsync(Options options)
        {
            if (options.ListDevices)
            {
                _PrintDevices();
                return 0;
            }

            MMDevice device = _GetDevice(options.SoundCard);
            if (device == null)
            {
                Console.WriteLine($"Invalid sound device index: {options.SoundCard}");
                Console.WriteLine("Available options are: ");
                _PrintDevices();
                return -1;
            }

            s_Connection = new ClientConnection(options.HostName, options.Port);
            await s_Connection.ConnectAsync();
            s_Capture = new WasapiLoopbackCapture(device);
            s_Capture.DataAvailable += _CaptureOnDataAvailable;
            s_Capture.StartRecording();

            while (true)
            {
                await Task.Delay(100);
            }
        }

        private static void _CaptureOnDataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] data = _ToPcm16(e.Buffer, e.BytesRecorded, s_Capture.WaveFormat);
            s_Connection.Write(data, data.Length);
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
            using var memStream = new MemoryStream(inputBuffer, 0, length);
            using var inputStream = new RawSourceWaveStream(memStream, format);

            // Convert the input stream to a WaveProvider in 16bit PCM format with sample rate of 48000 Hz.
            var convertedPCM = new SampleToWaveProvider16(
                new WdlResamplingSampleProvider(
                    new WaveToSampleProvider(inputStream),
                    48000)
            );

            byte[] convertedBuffer = new byte[length];

            using var stream = new MemoryStream();
            int read;

            // Read the converted WaveProvider into a buffer and turn it into a Stream.
            while ((read = convertedPCM.Read(convertedBuffer, 0, length)) > 0)
                stream.Write(convertedBuffer, 0, read);

            // Return the converted Stream as a byte array.
            return stream.ToArray();
        }

        private static MMDevice _GetDevice(int index)
        {
            List<MMDevice> devices = _GetWasapiDevices();
            if (index < devices.Count)
            {
                return devices[index];
            }
            return null;
        }

        private static List<MMDevice> _GetWasapiDevices()
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

        private static void _PrintDevices()
        {
            List<MMDevice> deviceList = _GetWasapiDevices();
            for (int i = 0; i < deviceList.Count; i++)
            {
                Console.WriteLine($"{i}: {deviceList[i].FriendlyName}");
            }
        }

        private static async Task<int> _HandleParseErrorAsync(IEnumerable<Error> errs)
        {
            //handle command line option errors
            foreach (Error e in errs)
            {
                if (e.Tag != ErrorType.HelpRequestedError && e.Tag != ErrorType.VersionRequestedError)
                {
                    Console.WriteLine(e.Tag);
                }
            }

            return -1;
        }
    }
}

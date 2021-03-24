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
    public class BroadcastController
    {
        private ClientConnection m_Connection;
        private WasapiLoopbackCapture m_Capture = null;
        private MMDevice m_Device = null;

        public event Action<bool> OnConnected = null;

        private bool m_Quit = false;

        public BroadcastController(MMDevice device)
        {
            m_Device = device;
        }

        public async Task RunAsync(string address, int port)
        {
            m_Connection = new ClientConnection(address, port);
            m_Connection.OnConnected += _OnConnected;
            await m_Connection.ConnectAsync();
            m_Capture = new WasapiLoopbackCapture(m_Device);
            m_Capture.DataAvailable += _CaptureOnDataAvailable;
            m_Capture.StartRecording();

            while (m_Quit == false)
            {
                await Task.Delay(100);
            }

            m_Connection.Dispose();
            m_Capture.Dispose();
        }

        public void Stop()
        {
            m_Quit = true;
            m_Connection.Stop();
        }

        private void _OnConnected(bool connected)
        {
            OnConnected?.Invoke(connected);
        }

        private void _CaptureOnDataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] data = _ToPcm16(e.Buffer, e.BytesRecorded, m_Capture.WaveFormat);
            m_Connection.Write(data, data.Length);
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

    }
}

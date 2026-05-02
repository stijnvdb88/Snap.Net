using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snap.Net.Broadcast
{
    public class BroadcastController
    {
        private ClientConnection m_Connection;
        
        private readonly IAudioDevice m_Device = null;

        public event Action<bool> OnConnected = null;
        public event Action<bool, string> OnCapturingAudio = null;

        private bool m_CapturingAudio = false;
        private bool m_Quit = false;

        public BroadcastController(IAudioDevice device)
        {
            m_Device = device;
        }

        public async Task RunAsync(string address, int port)
        {
            m_Connection = new ClientConnection(address, port);
            m_Connection.OnConnected += _OnConnected;
            await m_Connection.ConnectAsync();

            
            OnCapturingAudio?.Invoke(false, m_Device.FriendlyName);
            m_Device.Start();
            m_Device.OnPcm16DataAvailable += _CaptureOnDataAvailable;
            while (m_Quit == false)
            {
                await Task.Delay(100);
            }

            m_Connection.Dispose();
            m_Device.Stop();
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

        private void _CaptureOnDataAvailable(object sender, byte[] audioCaptureData)
        {
            bool capturingAudio = audioCaptureData.Length > 0;
            if (capturingAudio != m_CapturingAudio)
            {
                OnCapturingAudio?.Invoke(capturingAudio, m_Device.FriendlyName);
            }
            m_CapturingAudio = capturingAudio;
            m_Connection.Write(audioCaptureData, audioCaptureData.Length);
        }
    }
}

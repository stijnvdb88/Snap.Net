using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Thread = Java.Lang.Thread;

namespace SnapDotNet.Mobile.Droid.Player
{
    public class ClientConnection : IDisposable
    {
        private TcpClient m_Socket = null;
        private NetworkStream m_Stream;

        public event Action<bool> OnConnected = null;

        private readonly string m_Address = "";
        private readonly int m_Port = 0;
        private int m_ConnectAttempts = 0;
        private bool m_Quit = false;

        public ClientConnection(string address, int port)
        {
            m_Address = address;
            m_Port = port;
        }

        public void Connect()
        {
            while ((m_Socket == null || m_Socket.Connected == false) && m_Quit == false) // reconnect loop
            {
                try
                {
                    m_Socket = new TcpClient(m_Address, m_Port);
                    m_Stream = m_Socket.GetStream();
                    OnConnected?.Invoke(true);
                }
                catch (Exception e)
                {
                    if (m_ConnectAttempts == 0)
                    {
                        OnConnected?.Invoke(false);
                    }
                    Console.WriteLine("Failed to connect. Exception: " + e.Message);
                    m_Socket?.Dispose();
                    m_Socket = null;

                    Thread.Sleep(100);
                }

                m_ConnectAttempts++;
            }
        }

        public void Stop()
        {
            m_Quit = true;
        }


        public void Write(byte[] data, int length)
        {
            try
            {
                m_Stream.Write(data, 0, length);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ClientConnection.Write exception: {e.Message}, starting reconnect loop");
                OnConnected?.Invoke(false);
            }
        }

        public void Dispose()
        {
            m_Socket?.Dispose();
            m_Stream?.Dispose();
        }
    }
}

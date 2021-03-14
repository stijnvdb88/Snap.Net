/*
    This file is part of Snap.Net
    Copyright (C) 2020  Stijn Van der Borght
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace Snap.Net.SnapClient
{
    public class ClientConnection : IDisposable
    {
        private TcpClient m_Socket = null;
        private NetworkStream m_Stream;

        private CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();
        private CancellationTokenSource m_ConnectionTimeOutTokenSource = new CancellationTokenSource();

        private ushort m_MessageIndex = 0; // we increment this from our end each time we send a message

        private MessageRouter m_MessageRouter = null;

        private string m_Address = "";
        private int m_Port = 0;

        public event Action OnConnected = null;

        public ClientConnection(MessageRouter messageRouter)
        {
            m_MessageRouter = messageRouter;
        }

        public async Task ConnectAsync(string address, int port)
        {
            m_Address = address;
            m_Port = port;
            while (m_Socket == null || m_Socket.Connected == false) // reconnect loop
            {
                try
                {
                    m_Socket = new TcpClient(address, port);
                    m_Stream = m_Socket.GetStream();
                    OnConnected?.Invoke();
                    //Console.WriteLine("connected");
                }
                catch
                {
                    //Console.WriteLine("connect fail");
                    m_Socket?.Dispose();
                    m_Socket = null;
                    await Task.Delay(100);
                }
            }

        }

        public async Task SendMessageAsync(BaseMessage message, double now)
        {
            if (m_Socket == null || m_Socket.Connected == false)
            {
                return;
            }

            message.Sent.SetMilliseconds(now);
            message.Id = m_MessageIndex;
            m_MessageIndex++;
            byte[] serialized = message.Serialize();
            await m_Stream.WriteAsync(serialized, 0, serialized.Length);
        }

        public async Task GetNextMessageAsync()
        {
            if (m_Socket == null || m_Socket.Connected == false)
            {
                return;
            }

            // The Snapcast protocol works as follows: all messages start with a message header (see BaseMessage)
            // This header has a "type" field to let us know what kind of message we're receiving, and also
            // a "size" field, to let us know how much more data we need to read to get to the end of this message.

            try
            {
                byte[] message = new byte[BaseMessage.BASE_SIZE];
                int read = 0;
                while (read < message.Length) // keep reading until we get a message header (26 bytes)
                {
                    int currRead = await m_Stream.ReadAsync(message, read, message.Length - read);
                    read += currRead;
                }

                // at this point we got a header, now read the rest
                BaseMessage baseMessage = new BaseMessage(message);
                byte[] restOfMessage = new byte[baseMessage.Size]; 
                read = 0;
                while (read < restOfMessage.Length)
                {
                    int bytesRead = await m_Stream.ReadAsync(restOfMessage, read, restOfMessage.Length - read);
                    read += bytesRead;
                }

                // compose the final message
                byte[] fullMessage = new byte[message.Length + restOfMessage.Length];
                Buffer.BlockCopy(message, 0, fullMessage, 0, message.Length);
                Buffer.BlockCopy(restOfMessage, 0, fullMessage, message.Length, restOfMessage.Length);

                // hand it off to the MessageRouter, which will send it where it needs to go (see Controller.StartAsync for subscriber list)
                m_MessageRouter.AddMessage(baseMessage.Type, fullMessage);
            }
            catch (IOException exception)
            {
                Console.WriteLine("Connection closed " + exception.Message);
                await ConnectAsync(m_Address, m_Port);
            }
        }

        public void Dispose()
        {
            m_Socket?.Dispose();
            m_Stream?.Dispose();
            m_CancellationTokenSource?.Dispose();
            m_ConnectionTimeOutTokenSource?.Dispose();
        }
    }
}
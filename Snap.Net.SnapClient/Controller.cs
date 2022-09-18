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

using Snap.Net.SnapClient.Decoder;
using Snap.Net.SnapClient.Message;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Snap.Net.SnapClient.Time;
using Timer = System.Threading.Timer;

namespace Snap.Net.SnapClient
{
    public class Controller : IMessageListener<CodecHeaderMessage>, IMessageListener<PcmChunkMessage>, IDisposable
    {
        private ClientConnection m_ClientConnection = null; // the connection with snapserver
        private Player.Player m_Player = null; // responsible for playing audio
        private Decoder.Decoder m_Decoder = null; // decodes incoming audio to pcm
        private TimeProvider m_TimeProvider = null; // keeps track of our time diff with the server
        private SampleFormat m_SampleFormat = null; // known after receiving the first codec header message

        private HelloMessage m_HelloMessage = null; // supplied by the user, this lets snapserver know what kind of device we are
        private MessageRouter m_MessageRouter = new MessageRouter(); // this class parses all incoming messages and forwards them where needed

        private Timer m_SyncTimer = null; // timer for sending periodic time messages to server

        private Dictionary<string, Decoder.Decoder> m_Decoders = new Dictionary<string, Decoder.Decoder>(); // available decoders

        private bool m_Running = true;

        public Controller(Player.Player player, HelloMessage helloMessage)
        {
            m_Player = player;
            m_TimeProvider = m_Player.GetTimeProvider();
            m_HelloMessage = helloMessage;
            m_ClientConnection = new ClientConnection(m_MessageRouter);
        }

        public async Task StartAsync(string address, int port)
        {
            m_ClientConnection.OnConnected += async () =>
            {
                // say hello once we're connected:
                await m_ClientConnection.SendMessageAsync(new JsonMessage<HelloMessage>(m_HelloMessage,
                    MessageRouter.EMessageType.Hello), m_TimeProvider.Now());
            };

            await m_ClientConnection.ConnectAsync(address, port);

            // first, we let the message router know which messages go where:
            m_MessageRouter.RegisterListener<CodecHeaderMessage>(this); // controller will deal with setting up the decoder, and then forward info to the player
            m_MessageRouter.RegisterListener<PcmChunkMessage>(this); // controller will first receive audio messages, decode them, and then forward to the player
            // (generic types could be omitted here, leaving them in anyway for readability)
            m_MessageRouter.RegisterListener<JsonMessage<ServerSettingsMessage>>(m_Player); // player will receive server settings messages directly (these will manipulate volume, mute, latency)
            m_MessageRouter.RegisterListener<TimeMessage>(m_TimeProvider); // time provider will receive time sync messages

            // register the available decoders
            m_Decoders.Add("flac", new FlacDecoder());
            m_Decoders.Add("pcm", new PcmDecoder());
            m_Decoders.Add("opus", new OpusDecoder());


            // start our periodic diff calculation (each second we ping-pong with the server to figure out the latency between us)
            m_SyncTimer = new Timer(_SyncTimerElapsed, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
            // start the connection loop!
            while (m_Running)
            {
                if (m_ClientConnection != null)
                {
                    await m_ClientConnection.GetNextMessageAsync();
                }
            }
        }

        public void Stop()
        {
            m_Running = false;
            m_Player.Stop();
        }

        /// <summary>
        /// Called when the server sends us a codec header
        /// </summary>
        /// <param name="message"></param>
        public void OnMessageReceived(CodecHeaderMessage message)
        {
            // if we already had a decoder set up, it means we were already playing audio.
            // this also means our Player will need to be re-initialized, as the sample format has changed.
            if (m_Decoder != null)
            {
                m_Player.Stop();
            }

            if (m_Decoders.ContainsKey(message.Codec))
            {
                m_Decoder = m_Decoders[message.Codec]; // grab the correct decoder
                m_SampleFormat = m_Decoder.SetHeader(message.Payload); // figure out the sample format from the codec header
                m_Player.Start(m_SampleFormat);
            }
            else
            {
                Console.WriteLine($"Codec '{message.Codec}' not supported!");
                // throw exception?
            }
        }

        /// <summary>
        /// Called when the server sends us an audio chunk
        /// </summary>
        /// <param name="message"></param>
        public void OnMessageReceived(PcmChunkMessage message)
        {
            if (m_Decoder != null)
            {
                message.SetSampleFormat(m_SampleFormat);
                PcmChunkMessage decoded = m_Decoder.Decode(message);
                m_Player.OnPcmChunkReceived(decoded);
            }
        }

        /// <summary>
        /// This function gets called each second, we just send a time message to the server each time
        /// see TimeProvider for the logic of handling the server's response
        /// </summary>
        /// <param name="state"></param>
        private async void _SyncTimerElapsed(object state)
        {
            TimeMessage timeMessage = new TimeMessage();
            timeMessage.Latency.SetMilliseconds(m_TimeProvider.Now());
            await m_ClientConnection.SendMessageAsync(timeMessage, m_TimeProvider.Now());
        }

        public void Dispose()
        {
            m_ClientConnection?.Dispose();
            m_Player?.Dispose();
            m_SyncTimer?.Dispose();
        }
    }
}

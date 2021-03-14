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

using Snap.Net.SnapClient.Message;
using System;
using System.Collections.Generic;

namespace Snap.Net.SnapClient
{
    public class MessageRouter
    {
        public enum EMessageType
        {
            CodecHeader = 1,
            WireChunk = 2,
            ServerSettings = 3,
            TimeSync = 4,
            Hello = 5,
            StreamTags = 6
        }

        private readonly Dictionary<Type, List<Delegate>> m_Listeners = new Dictionary<Type, List<Delegate>>();

        public void AddMessage(EMessageType messageType, byte[] fullMessage)
        {
            switch (messageType)
            {

                case EMessageType.CodecHeader:
                    _Notify(new CodecHeaderMessage(fullMessage));
                    break;

                case EMessageType.WireChunk:
                    _Notify(new PcmChunkMessage(fullMessage));
                    break;

                case EMessageType.ServerSettings:
                    _Notify(new JsonMessage<ServerSettingsMessage>(fullMessage));
                    break;

                case EMessageType.TimeSync:
                    _Notify(new TimeMessage(fullMessage));
                    break;

                case EMessageType.StreamTags:
                    _Notify(new JsonMessage<StreamTagsMessage>(fullMessage));
                    break;
            }
        }

        private void _Notify<T>(T message)
        {
            if (m_Listeners.ContainsKey(typeof(T)))
            {
                foreach (Delegate onMessage in m_Listeners[typeof(T)])
                {
                    Action<T> action = onMessage as Action<T>;
                    action?.Invoke(message);
                }
            }
        }

        public void RegisterListener<T>(IMessageListener<T> listener) where T : BaseMessage
        {
            if (m_Listeners.ContainsKey(typeof(T)) == false)
            {
                m_Listeners.Add(typeof(T), new List<Delegate>());
            }

            Action<T> action = listener.OnMessageReceived;
            m_Listeners[typeof(T)].Add(action);
        }
    }
}

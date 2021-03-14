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

namespace Snap.Net.SnapClient
{
    [System.Serializable]
    public class BaseMessage
    {
        public MessageRouter.EMessageType Type => (MessageRouter.EMessageType)m_Type;

        public ushort Id
        {
            get => m_Id;
            set => m_Id = value;
        }

        public TimeValue Sent => m_Sent;
        public TimeValue Received => m_Received;
        public uint Size => m_Size;

        protected ushort m_Type; // 2 (2)
        private ushort m_Id; // 2 (4)
        private ushort m_RefersTo; // 2 (6)
        private TimeValue m_Sent = new TimeValue(0, 0); // 8 (14)
        private TimeValue m_Received = new TimeValue(0, 0); // 8 (22)
        private uint m_Size = 0; // 4 (26)

        public const int BASE_SIZE = 26;

        public BaseMessage()
        {
        }

        public BaseMessage(byte[] buffer)
        {
            this.Deserialize(buffer);
        }

        public virtual void Deserialize(byte[] buffer)
        {
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(memoryStream))
                {
                    m_Type = reader.ReadUInt16();
                    m_Id = reader.ReadUInt16();
                    m_RefersTo = reader.ReadUInt16();

                    byte[] sentBytes = new byte[8];
                    memoryStream.Read(sentBytes, 0, sentBytes.Length);
                    m_Sent = new TimeValue(sentBytes);

                    byte[] receivedBytes = new byte[8];
                    memoryStream.Read(receivedBytes, 0, receivedBytes.Length);
                    m_Received = new TimeValue(receivedBytes);

                    m_Size = reader.ReadUInt32();
                }
            }
        }

        public virtual byte[] Serialize()
        {
            using (MemoryStream memoryStream = new MemoryStream(BASE_SIZE))
            {
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    m_Size = GetSize();

                    writer.Write(m_Type);
                    writer.Write(m_Id);
                    writer.Write(m_RefersTo);
                    byte[] sentBytes = m_Sent.Serialize();
                    memoryStream.Write(sentBytes, 0, sentBytes.Length);

                    byte[] receivedBytes = m_Received.Serialize();
                    memoryStream.Write(receivedBytes, 0, receivedBytes.Length);

                    writer.Write(m_Size);
                }
                return memoryStream.GetBuffer();
            }
        }

        public virtual UInt32 GetSize()
        {
            return 0;
        }
    }
}

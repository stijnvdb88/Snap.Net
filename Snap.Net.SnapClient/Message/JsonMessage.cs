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

using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Snap.Net.SnapClient
{
    [System.Serializable]
    public class JsonMessage<T> : BaseMessage
    {
        private string m_Json = "";

        public JsonMessage(T data, MessageRouter.EMessageType type)
        {
            m_Json = JsonConvert.SerializeObject(data);
            m_Type = (ushort)type;
        }

        public JsonMessage(byte[] buffer)
        {
            Deserialize(buffer);
        }

        public override void Deserialize(byte[] buffer)
        {
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                byte[] baseData = new byte[26];
                memoryStream.Read(baseData, 0, baseData.Length);

                base.Deserialize(baseData);

                byte[] sizeBytes = new byte[4];
                memoryStream.Read(sizeBytes, 0, sizeBytes.Length);
                uint jsonSize = BitConverter.ToUInt32(sizeBytes, 0);

                byte[] jsonBytes = new byte[jsonSize];
                memoryStream.Read(jsonBytes, 0, jsonBytes.Length);
                m_Json = Encoding.UTF8.GetString(jsonBytes);
            }
        }

        public override byte[] Serialize()
        {
            using (MemoryStream memoryStream = new MemoryStream(BASE_SIZE + (int)GetSize()))
            {
                byte[] data = base.Serialize();
                memoryStream.Write(data, 0, data.Length);

                uint jsonSize = (uint)m_Json.Length;
                byte[] sizeBytes = BitConverter.GetBytes(jsonSize);
                memoryStream.Write(sizeBytes, 0, sizeBytes.Length);

                byte[] jsonBytes = Encoding.UTF8.GetBytes(m_Json);
                memoryStream.Write(jsonBytes, 0, jsonBytes.Length);

                return memoryStream.GetBuffer();
            }
        }

        public T GetData()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(m_Json);
        }

        public override uint GetSize()
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(m_Json);
            return (uint)jsonBytes.Length + 4; // 4 = Uint32 length
        }
    }
}

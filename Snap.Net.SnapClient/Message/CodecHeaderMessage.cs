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

using System.IO;
using System.Text;

namespace Snap.Net.SnapClient
{
    [System.Serializable]
    public class CodecHeaderMessage : BaseMessage
    {
        public string Codec => m_Codec;
        public byte[] Payload => m_Payload;

        private string m_Codec = "";

        private uint m_PayloadSize = 0;
        private byte[] m_Payload;

        public CodecHeaderMessage(byte[] buffer)
        {
            Deserialize(buffer);
        }

        public sealed override void Deserialize(byte[] buffer)
        {
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                byte[] baseData = new byte[26];
                memoryStream.Read(baseData, 0, baseData.Length);

                base.Deserialize(baseData);

                using (BinaryReader reader = new BinaryReader(memoryStream))
                {
                    uint codecSize = reader.ReadUInt32();
                    byte[] codecBytes = new byte[codecSize];
                    memoryStream.Read(codecBytes, 0, codecBytes.Length);

                    m_Codec = Encoding.UTF8.GetString(codecBytes);
                    m_PayloadSize = reader.ReadUInt32();

                    m_Payload = new byte[m_PayloadSize];
                    memoryStream.Read(m_Payload, 0, m_Payload.Length);
                }
            }
        }
    }
}

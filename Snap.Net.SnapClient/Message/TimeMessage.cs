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

namespace Snap.Net.SnapClient
{
    [System.Serializable]
    public class TimeMessage : BaseMessage
    {
        public TimeValue Latency => m_Latency;
        private TimeValue m_Latency = new TimeValue(0, 0);

        public TimeMessage(byte[] buffer)
        {
            Deserialize(buffer);
        }

        public TimeMessage()
        {
            m_Type = 4;
        }

        public override void Deserialize(byte[] buffer)
        {
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                byte[] baseData = new byte[26];
                memoryStream.Read(baseData, 0, baseData.Length);

                base.Deserialize(baseData);

                byte[] latencyBytes = new byte[8];
                memoryStream.Read(latencyBytes, 0, latencyBytes.Length);
                m_Latency = new TimeValue(latencyBytes);
            }
        }

        public override byte[] Serialize()
        {
            using (MemoryStream memoryStream = new MemoryStream(BASE_SIZE + (int)GetSize()))
            {
                byte[] data = base.Serialize();
                memoryStream.Write(data, 0, data.Length);

                byte[] latencyBytes = m_Latency.Serialize();
                memoryStream.Write(latencyBytes, 0, latencyBytes.Length);

                return memoryStream.GetBuffer();
            }
        }

        public override uint GetSize()
        {
            return 8;
        }
    }
}

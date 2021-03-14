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

namespace Snap.Net.SnapClient.Message
{
    [System.Serializable]
    public class PcmChunkMessage : BaseMessage
    {
        public TimeValue Timestamp => m_Timestamp;
        public byte[] Payload => m_Payload;

        private TimeValue m_Timestamp = new TimeValue(0, 0);
        private byte[] m_Payload = null;

        private SampleFormat m_SampleFormat = null;
        private int m_Idx = 0;

        public SampleFormat SampleFormat => m_SampleFormat;

        public long StartMs => m_StartMs;
        private long m_StartMs;

        public long Duration => m_Duration;
        private long m_Duration;

        private int m_FrameCount = 0;

        public PcmChunkMessage(byte[] buffer)
        {
            Deserialize(buffer);
        }

        public void SetSampleFormat(SampleFormat sampleFormat)
        {
            m_SampleFormat = sampleFormat;
            // we calculate these values beforehand as they get requested often on the hot path:
            float add = 1000 * (m_Idx / (float)m_SampleFormat.Rate);
            m_StartMs = (long)m_Timestamp.GetMilliseconds() + (long)add;

            m_FrameCount = m_Payload.Length / m_SampleFormat.FrameSize;

            float fr = (((float)m_FrameCount - m_Idx) / (float)m_SampleFormat.Rate);
            m_Duration = (int)(1000.0f * fr);
        }


        public byte[] ReadFrames(int frames)
        {
            if (m_Payload.Length == 0)
            {
                m_Idx += frames;
                return new byte[frames * m_SampleFormat.FrameSize];
            }

            int frameCount = frames;
            int frameSize = m_SampleFormat.FrameSize;
            if (m_Idx + frames > m_Payload.Length / frameSize)
            {
                frameCount = (m_Payload.Length / frameSize) - m_Idx;
            }

            int begin = m_Idx * frameSize;
            m_Idx += frameCount;
            int end = begin + frameCount * frameSize;

            byte[] result = new byte[frameCount * frameSize];
            Buffer.BlockCopy(m_Payload, begin, result, 0, frameCount * frameSize);
            return result;
        }

        public bool IsEndOfChunk()
        {
            return m_Idx >= m_FrameCount;
        }

        public void ClearPayload()
        {
            m_Payload = new byte[0];
        }

        public void SetPayload(byte[] newPayload)
        {
            m_Payload = newPayload;
            m_FrameCount = m_Payload.Length / m_SampleFormat.FrameSize;
            float fr = (((float)m_FrameCount - m_Idx) / (float)m_SampleFormat.Rate);
            m_Duration = (int)(1000.0f * fr);
        }

        public override void Deserialize(byte[] buffer)
        {
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                byte[] baseData = new byte[26];
                memoryStream.Read(baseData, 0, baseData.Length);

                base.Deserialize(baseData);

                byte[] timestampBytes = new byte[8];
                memoryStream.Read(timestampBytes, 0, timestampBytes.Length);
                m_Timestamp = new TimeValue(timestampBytes);

                byte[] sizeBytes = new byte[4];
                memoryStream.Read(sizeBytes, 0, sizeBytes.Length);
                uint payloadSize = BitConverter.ToUInt32(sizeBytes, 0);

                m_Payload = new byte[payloadSize];
                memoryStream.Read(m_Payload, 0, m_Payload.Length);
            }
        }
    }
}

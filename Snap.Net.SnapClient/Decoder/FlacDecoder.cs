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

using NAudio.Flac;
using Snap.Net.SnapClient.Message;
using System;
using System.IO;

namespace Snap.Net.SnapClient.Decoder
{
    class FlacDecoder : Decoder
    {
        private byte[] m_FlacHeader = null;

        public override SampleFormat SetHeader(byte[] buffer)
        {
            m_FlacHeader = buffer;
            MemoryStream stream = new MemoryStream(buffer);
            using (FlacReader reader = new FlacReader(stream))
            {
                return SampleFormat.FromWaveFormat(reader.WaveFormat);
            }
        }

        public override PcmChunkMessage Decode(PcmChunkMessage flacChunk)
        {
            byte[] allData = new byte[m_FlacHeader.Length + flacChunk.Payload.Length];

            Buffer.BlockCopy(m_FlacHeader, 0, allData, 0, m_FlacHeader.Length);
            Buffer.BlockCopy(flacChunk.Payload, 0, allData, m_FlacHeader.Length, flacChunk.Payload.Length);

            using (MemoryStream stream = new MemoryStream(allData))
            {
                using (FlacReader reader = new FlacReader(stream))
                {
                    byte[] newPayload = new byte[reader.Length];
                    reader.Read(newPayload, 0, newPayload.Length); // this is leaking memory
                    flacChunk.ClearPayload();
                    flacChunk.SetPayload(newPayload);
                }
            }
            return flacChunk;
        }
    }
}
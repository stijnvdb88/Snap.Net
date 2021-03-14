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
using System.IO;

namespace Snap.Net.SnapClient.Decoder
{
    class PcmDecoder : Decoder
    {
        public override SampleFormat SetHeader(byte[] buffer)
        {
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                byte[] channelBytes = new byte[2];
                memoryStream.Position = 22;
                memoryStream.Read(channelBytes, 0, channelBytes.Length);
                UInt16 channels = BitConverter.ToUInt16(channelBytes, 0);

                byte[] rateBytes = new byte[4];
                memoryStream.Read(rateBytes, 0, rateBytes.Length);
                UInt32 rate = BitConverter.ToUInt32(rateBytes, 0);

                byte[] bitsBytes = new byte[2];
                memoryStream.Position = 34;
                memoryStream.Read(bitsBytes, 0, bitsBytes.Length);
                UInt16 bits = BitConverter.ToUInt16(bitsBytes, 0);

                return new SampleFormat((int)rate, channels, bits);
            }
        }

        public override PcmChunkMessage Decode(PcmChunkMessage chunk)
        {
            return chunk;
        }
    }
}

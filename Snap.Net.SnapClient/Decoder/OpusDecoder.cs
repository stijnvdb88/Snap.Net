using System;
using System.Collections.Generic;
using System.Text;
using Snap.Net.SnapClient.Message;
using System.IO;
using Concentus.Oggfile;
using Concentus.Structs;

namespace Snap.Net.SnapClient.Decoder
{
    class OpusDecoder : Decoder
    {
        const UInt32 OPUS_ID = 0x4F505553; // magic number for verifying opus header
        
        private Concentus.Structs.OpusDecoder m_OpusDecoder = null;
        private SampleFormat m_SampleFormat = null;

        public override SampleFormat SetHeader(byte[] buffer)
        {
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                byte[] opusIdBytes = new byte[4];
                memoryStream.Read(opusIdBytes, 0, opusIdBytes.Length);
                UInt32 opusId = BitConverter.ToUInt32(opusIdBytes, 0);

                if(opusId != OPUS_ID)
                {
                    throw new Exception("Not an Opus header");
                }

                byte[] rateBytes = new byte[4];
                memoryStream.Read(rateBytes, 0, rateBytes.Length);
                UInt32 rate = BitConverter.ToUInt32(rateBytes, 0);

                byte[] bitsBytes = new byte[2];
                memoryStream.Read(bitsBytes, 0, bitsBytes.Length);
                UInt16 bits = BitConverter.ToUInt16(bitsBytes, 0);

                byte[] channelBytes = new byte[2];
                memoryStream.Read(channelBytes, 0, channelBytes.Length);
                UInt16 channels = BitConverter.ToUInt16(channelBytes, 0);

                m_OpusDecoder = new Concentus.Structs.OpusDecoder((int)rate, channels);

                m_SampleFormat = new SampleFormat((int)rate, channels, bits);
                return m_SampleFormat;
            }
        }

        public override PcmChunkMessage Decode(PcmChunkMessage chunk)
        {
            int numSamples = OpusPacketInfo.GetNumSamples(chunk.Payload, 0, chunk.Payload.Length, m_SampleFormat.Rate);
            short[] pcmOut = new short[numSamples * m_SampleFormat.Channels];
            m_OpusDecoder.Decode(chunk.Payload, 0, chunk.Payload.Length, pcmOut, 0, numSamples);
            chunk.ClearPayload();
            chunk.SetPayload(_ShortsToBytes(pcmOut, 0, pcmOut.Length));
            return chunk;
        }

        /// <summary>
        /// Converts linear short samples into interleaved byte samples, for writing to a file, waveout device, etc.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static byte[] _ShortsToBytes(short[] input, int in_offset, int samples)
        {
            byte[] processedValues = new byte[samples * 2];
            _ShortsToBytes(input, in_offset, processedValues, 0, samples);
            return processedValues;
        }

        /// <summary>
        /// Converts linear short samples into interleaved byte samples, for writing to a file, waveout device, etc.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static void _ShortsToBytes(short[] input, int in_offset, byte[] output, int out_offset, int samples)
        {
            for (int c = 0; c < samples; c++)
            {
                output[(c * 2) + out_offset] = (byte)(input[c + in_offset] & 0xFF);
                output[(c * 2) + out_offset + 1] = (byte)((input[c + in_offset] >> 8) & 0xFF);
            }
        }
    }
}

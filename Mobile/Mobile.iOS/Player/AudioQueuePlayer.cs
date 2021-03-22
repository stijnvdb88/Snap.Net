using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AudioToolbox;
using AVFoundation;
using Foundation;
using Snap.Net.SnapClient;
using UIKit;
using System.Diagnostics;
using Snap.Net.SnapClient.Time;

namespace SnapDotNet.Mobile.iOS.Player
{
    class AudioQueuePlayer : Snap.Net.SnapClient.Player.Player
    {
        private OutputAudioQueue m_AudioQueue = null;

        private int m_BufferDurationMs = 0;
        private int m_OffsetToleranceMs = 0;

        private int m_BufferFrameCount = 0;
        private int m_NumBuffers = 3;

        private AudioQueueTimeProvider m_AudioQueueTimeProvider = null;

        public AudioQueuePlayer(int bufferDurationMs, int offsetToleranceMs)
        {
            m_BufferDurationMs = bufferDurationMs;
            m_OffsetToleranceMs = offsetToleranceMs;
        }

        public override void Start(SampleFormat sampleFormat)
        {
            m_SampleFormat = sampleFormat;
            m_AudioStream = new AudioStream(GetTimeProvider(), m_SampleFormat, m_BufferDurationMs, m_OffsetToleranceMs);
            m_BufferFrameCount = (int)Math.Floor(m_BufferDurationMs * m_SampleFormat.MsRate);
            AudioStreamBasicDescription format = new AudioStreamBasicDescription
            {
                SampleRate = sampleFormat.Rate,
                Format = AudioFormatType.LinearPCM,
                FormatFlags = AudioFormatFlags.LinearPCMIsSignedInteger | AudioFormatFlags.LinearPCMIsPacked,
                BitsPerChannel = sampleFormat.Bits,
                ChannelsPerFrame = sampleFormat.Channels,
                BytesPerFrame = sampleFormat.FrameSize,
                BytesPerPacket = sampleFormat.FrameSize,
                FramesPerPacket = 1
            };
            m_AudioQueue = new OutputAudioQueue(format);
            m_AudioQueueTimeProvider.Init(m_AudioQueue, m_SampleFormat);
            m_AudioQueue.Volume = m_Volume;
            int bufferSize = m_BufferFrameCount * m_SampleFormat.FrameSize;
            _UnsafeStart(bufferSize);
            m_AudioQueue.Start();
        }

        public override TimeProvider GetTimeProvider()
        {
            return m_AudioQueueTimeProvider ?? (m_AudioQueueTimeProvider = new AudioQueueTimeProvider());
        }

        private unsafe void _UnsafeStart(int bufferSize)
        {
            AudioQueueBuffer*[] buffers = new AudioQueueBuffer*[m_NumBuffers];
            for (int i = 0; i < m_NumBuffers; i++)
            {
                m_AudioQueue.AllocateBuffer(bufferSize, out buffers[i]);
                _PlayNext(buffers[i], m_BufferDurationMs * i);
            }

            m_AudioQueue.BufferCompleted += (object sender, BufferCompletedEventArgs e) =>
            {
                _PlayNext(e.UnsafeBuffer, m_BufferDurationMs * (m_NumBuffers - 1));
            };

        }

        private unsafe void _PlayNext(AudioQueueBuffer* buffer, int offset)
        {
            byte[] data = m_AudioStream.GetNextBuffer(m_SampleFormat, m_BufferFrameCount, m_BufferMs - offset, m_DacLatency);
            Marshal.Copy(data, 0, buffer->AudioData, data.Length);
            buffer->AudioDataByteSize = buffer->AudioDataBytesCapacity;
            m_AudioQueue.EnqueueBuffer(buffer, null);
        }

        public override void OnMessageReceived(JsonMessage<ServerSettingsMessage> message)
        {
            base.OnMessageReceived(message);
            if (m_AudioQueue != null)
            {
                m_AudioQueue.Volume = m_Volume;
            }
        }

        public override void Stop()
        {
            m_AudioQueue.Stop(true);
        }


        public override void Dispose()
        {
            m_AudioQueue.Dispose();
        }
    }
}

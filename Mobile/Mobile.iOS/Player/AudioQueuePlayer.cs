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
        private int m_NumBuffers = 3;
        private AudioQueueTimeProvider m_AudioQueueTimeProvider = null;

        public AudioQueuePlayer(int bufferDurationMs, int offsetToleranceMs) : base(
            0, // 0: this Player implementation passes in the latency itself via _PlayNext's offset parameter
            bufferDurationMs, 
            offsetToleranceMs)
        {
        }
        
        protected override void _Start()
        {
            AudioStreamBasicDescription format = new AudioStreamBasicDescription
            {
                SampleRate = m_SampleFormat.Rate,
                Format = AudioFormatType.LinearPCM,
                FormatFlags = AudioFormatFlags.LinearPCMIsSignedInteger | AudioFormatFlags.LinearPCMIsPacked,
                BitsPerChannel = m_SampleFormat.Bits,
                ChannelsPerFrame = m_SampleFormat.Channels,
                BytesPerFrame = m_SampleFormat.FrameSize,
                BytesPerPacket = m_SampleFormat.FrameSize,
                FramesPerPacket = 1
            };

            m_AudioQueue = new OutputAudioQueue(format);
            m_AudioQueueTimeProvider.Init(m_AudioQueue, m_SampleFormat);
            _OnSettingsUpdated();
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

        protected override void _OnSettingsUpdated()
        {
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

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

using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Threading;

namespace Snap.Net.SnapClient.Player
{
    public class NAudioPlayer : Player
    {
        // function which creates our IWavePlayer - we need this as we sometimes need to recreate the player ourselves (switching streams, restarting, etc)
        protected Func<IWavePlayer> m_OutputDeviceFactory = null;
        private BufferedWaveProvider m_OutputBuffer = null; // this is the buffer we'll feed samples into, the audio device will play these as they get filled
        private int m_BufferFrameCount = 0; // number of frames we'll feed into the buffer with each iteration (this is calculated based on OutputBufferMs and SampleFormat)
        private IWavePlayer m_OutputDevice = null; 

        private VolumeSampleProvider m_VolumeSampleProvider = null; // utility for manipulating volume (just multiplies each sample by volume)

        private Thread m_AudioThread = null;
        private int m_OffsetToleranceMs = 50;

        /// <summary>
        /// Class for playing back audio into a user supplied IWavePlayer, using an NAudio BufferedWaveProvider
        /// </summary>
        /// <param name="outputDeviceFactory">Func for creating the output device</param>
        /// <param name="dacLatency">Output device latency</param>
        /// <param name="bufferDurationMs">Length of our output buffer (ms)</param>
        /// <param name="offsetToleranceMs">How far we can be off before resorting to a hard sync (this causes an audible pop)</param>
        public NAudioPlayer(Func<IWavePlayer> outputDeviceFactory, int dacLatency, int bufferDurationMs, int offsetToleranceMs)
        {
            m_OutputDeviceFactory = outputDeviceFactory;
            m_DacLatency = dacLatency;
            m_BufferDurationMs = bufferDurationMs;
            m_OffsetToleranceMs = offsetToleranceMs;
        }

        public override void Start(TimeProvider timeProvider, SampleFormat sampleFormat)
        {
            m_OutputDevice = m_OutputDeviceFactory();
            m_SampleFormat = sampleFormat;

            m_AudioStream = new AudioStream(timeProvider, m_SampleFormat, m_BufferDurationMs, m_OffsetToleranceMs);

            m_BufferFrameCount = (int)Math.Floor(m_BufferDurationMs * m_SampleFormat.MsRate);

            m_OutputBuffer = new BufferedWaveProvider(m_SampleFormat.ToWaveFormat());
            m_OutputBuffer.BufferLength = m_BufferFrameCount * m_SampleFormat.FrameSize * 2;
            m_OutputBuffer.ReadFully = true; // keeps the audio device playing silence while we're not sending any data

            m_VolumeSampleProvider = new VolumeSampleProvider(m_OutputBuffer.ToSampleProvider());
            _UpdateVolume();

            m_OutputDevice.Init(m_VolumeSampleProvider);
            m_OutputDevice.Play();

            m_AudioThread = new Thread(_PlayLoop);
            m_AudioThread.Name = "Audio player thread";
            m_AudioThread.Priority = ThreadPriority.Highest;
            m_AudioThread.Start();
        }

        public override void Stop()
        {
            m_AudioThread.Abort();
            m_OutputDevice.Stop();
            m_OutputDevice.Dispose();
            m_OutputDevice = null;
        }

        public override void OnMessageReceived(JsonMessage<ServerSettingsMessage> message)
        {
            base.OnMessageReceived(message);
            _UpdateVolume();
        }

        private void _UpdateVolume()
        {
            if (m_VolumeSampleProvider != null)
            {
                m_VolumeSampleProvider.Volume = m_Volume;
            }
        }

        protected override void _PlayLoop()
        {
            try
            {
                _PlayNext();
                while (true)
                {
                    // this approach is slightly different from the original implementation:
                    // if we wait for the output buffer to completely run out of audio, the next buffer isn't fed in quickly enough and playback isn't smooth.
                    // instead, we wait for the output buffer to have less than a full buffer left, and we go ahead and add the next buffer to the back.
                    // this means we are constantly playing with a latency of m_BufferDurationMs (which is why we compensate for it in our call to GetNextBuffer later)
                    if (m_OutputBuffer.BufferedBytes <= (m_BufferFrameCount * m_SampleFormat.FrameSize))
                    {
                        _PlayNext();
                    }

                    Thread.Sleep(1);
                }
            }
            catch (ThreadAbortException exception)
            {
                Console.WriteLine($"NAudioPlayer: thread aborted. {exception.Message}");
            }
        }

        protected override void _PlayNext()
        {
            // this is where we ask for the next buffer worth of audio. m_BufferMs already has our client's latency factored in, as set through the RPC protocol
            // m_BufferDurationMs is compensated for because of the way we're feeding audio in (see comment block in the audio loop)
            byte[] buffer = m_AudioStream.GetNextBuffer(m_SampleFormat, m_BufferFrameCount, m_BufferMs - m_BufferDurationMs, m_DacLatency);
            m_OutputBuffer.AddSamples(buffer, 0, buffer.Length);
        }

        public override void Dispose()
        {
            m_OutputDevice?.Dispose();
        }
    }
}

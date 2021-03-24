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
using Snap.Net.SnapClient.Time;

namespace Snap.Net.SnapClient.Player
{
    /// <summary>
    /// Inherit from this class if you want to create your own player which does not use NAudio
    /// </summary>
    public abstract class Player : IMessageListener<JsonMessage<ServerSettingsMessage>>, IDisposable
    {
        protected AudioStream m_AudioStream = null;
        protected SampleFormat m_SampleFormat = null;
        protected TimeProvider m_TimeProvider = null;

        protected int m_BufferDurationMs = 0;
        protected int m_BufferFrameCount = 0; // number of frames we'll feed into the buffer with each iteration (this is calculated based on OutputBufferMs and SampleFormat)
        protected int m_OffsetToleranceMs = 50;

        protected int m_BufferMs = 0;
        protected float m_Volume = 0;
        protected bool m_Muted = false;
        protected int m_DacLatency = 0;

        protected Player(int dacLatency, int bufferDurationMs, int offsetToleranceMs)
        {
            m_DacLatency = dacLatency;
            m_BufferDurationMs = bufferDurationMs;
            m_OffsetToleranceMs = offsetToleranceMs;
        }

        public void Start(SampleFormat sampleFormat)
        {
            m_SampleFormat = sampleFormat;
            m_AudioStream = new AudioStream(GetTimeProvider(), m_SampleFormat, m_BufferDurationMs, m_OffsetToleranceMs);
            m_BufferFrameCount = (int)Math.Floor(m_BufferDurationMs * m_SampleFormat.MsRate);
            _Start();
        }

        protected abstract void _Start();
        public abstract void Stop();

        public void OnMessageReceived(JsonMessage<ServerSettingsMessage> message)
        {
            ServerSettingsMessage settings = message.GetData();
            m_Volume = settings.volume / 100.0f;
            m_Muted = settings.muted;
            m_BufferMs = settings.bufferMs - settings.latency;

            _OnSettingsUpdated();
        }

        /// <summary>
        /// Called after we've received new settings from the server.
        /// The player should update his volume and mute state here
        /// </summary>
        protected abstract void _OnSettingsUpdated();

        public void OnPcmChunkReceived(PcmChunkMessage decodedChunkMessage)
        {
            m_AudioStream.AddChunk(decodedChunkMessage);
        }
        
        /// <summary>
        /// Override this function if your player has access to a more accurate clock than Stopwatch
        /// e.g. audio engine clock
        /// </summary>
        /// <returns></returns>
        public virtual TimeProvider GetTimeProvider()
        {
            return m_TimeProvider ?? (m_TimeProvider = new StopwatchTimeProvider());
        }

        public abstract void Dispose();
    }
}

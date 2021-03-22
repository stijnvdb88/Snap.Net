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
using System.Collections.Generic;
using Snap.Net.SnapClient.Time;

namespace Snap.Net.SnapClient
{
    public class AudioStream
    {
        private TimeProvider m_TimeProvider = null; 
        private SampleFormat m_SampleFormat = null;
        private float m_BufferMs = 0;
        private Queue<PcmChunkMessage> m_Chunks = new Queue<PcmChunkMessage>(); // the queue we'll read our audio chunks off of
        private PcmChunkMessage m_Chunk = null; // the chunk currently being read
        private int m_OffsetToleranceMs = 5; // how far we can be off before resorting to a hard sync (this causes an audible pop)

        public AudioStream(TimeProvider timeProvider, SampleFormat sampleFormat, float bufferMs, int offsetToleranceMs)
        {
            m_TimeProvider = timeProvider;
            m_SampleFormat = sampleFormat;
            m_BufferMs = bufferMs;
            m_OffsetToleranceMs = offsetToleranceMs;
        }

        /// <summary>
        /// Queues up a chunk of audio sent to us by the server
        /// We also check our queue here to make sure it doesn't fill up with old chunks
        /// </summary>
        /// <param name="chunk"></param>
        public void AddChunk(PcmChunkMessage chunk)
        {
            lock (m_Chunks)
            {
                m_Chunks.Enqueue(chunk);
            }

            lock (m_Chunks)
            {
                while (m_Chunks.Count > 0)
                {
                    double age = m_TimeProvider.ServerNow() - m_Chunks.Peek().Timestamp.GetMilliseconds();
                    if (age > 5000 + m_BufferMs)
                    {
                        PcmChunkMessage dropping = m_Chunks.Peek();
                        //Console.WriteLine(
                         //   $"Dropping old chunk: {age}, left: {m_Chunks.Count}. Server now: {m_TimeProvider.ServerNow()} chunk time {m_Chunks.Peek().Timestamp.GetMilliseconds()}");
                        m_Chunks.Dequeue();
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// This function is the one where all the cool stuff happens :)
        /// It returns the requested number of audio frames, with time compensation for the latency values (server buffer, local buffer, DAC latency)
        /// </summary>
        /// <param name="format">The format being used</param>
        /// <param name="bufferFrameCount">The number of frames the audio device wants</param>
        /// <param name="bufferMs">Server buffer minus our local player buffer</param>
        /// <param name="dacLatency">The dac latency</param>
        /// <returns>The requested number of audio frames for the supplied playTime</returns>
        public byte[] GetNextBuffer(SampleFormat format, int bufferFrameCount, int bufferMs, int dacLatency)
        {
            lock (m_Chunks)
            {
                // if we don't currently have a chunk ready to read from, grab the first one from the queue
                if (m_Chunks.Count > 0 && m_Chunk == null)
                {
                    m_Chunk = m_Chunks.Dequeue();
                }
            }

            int frames = bufferFrameCount; // number of frames we'll send to the audio device
            if (m_Chunk != null)
            {
                double serverPlayTimeMs = m_TimeProvider.ServerNow() - bufferMs + dacLatency;
                double age = serverPlayTimeMs - m_Chunk.StartMs(); // age = server time - the time the server says this chunk needs to start playing
                double requestedFramesDurationMs = frames / m_SampleFormat.MsRate; // duration in milliseconds of the frames being requested
                int read = 0; // this will hold the number of frames we've read from the queued chunks (we often get asked to read more frames than a single chunk holds, which means we need to read across chunks)
                int pos = 0; // this will keep track of our position within the output buffer as we write to it (in bytes)
                if (age < -requestedFramesDurationMs)
                {
                    // eg. we're being asked to play 80ms worth of audio, and the current chunk is 80ms too young,
                    // in this case just return 80ms of silence (which puts us in sync next time this function gets called)
                    return new byte[bufferFrameCount * format.FrameSize];
                }

                //Console.WriteLine(age); // this value is the best indicator we have to measure how tight our audio loop is. the less this value strays from zero, the better

                // if the current chunk is (x)ms too young or old, seek to the correct position (seeking causes audible pops, but can't be helped if we somehow get this far out of sync)
                if (Math.Abs(age) > m_OffsetToleranceMs)
                {
                    // first, just skip through chunks as long as they're too old (eg if this chunk lasts 16ms but it's 16ms too old, just skipping this one puts us back in sync) 
                    while (m_Chunk != null && age > m_Chunk.Duration())
                    {
                        // this chunk is too old, just skip it
                        //Console.WriteLine("chunk too old, dropping (age: " + age + " > " + m_Chunk.Duration() + ") " + m_Chunk.StartMs());
                        lock (m_Chunks)
                        {
                            if (m_Chunks.Count == 0) // no chunks left, return silence
                            {
                                m_Chunk = null;
                                return new byte[bufferFrameCount * format.FrameSize];
                            }
                            m_Chunk = m_Chunks.Dequeue();
                        }
                        age = serverPlayTimeMs - m_Chunk.StartMs();
                    }

                    // at this point we've got the right chunk, now we just need to seek to the correct position inside of it
                    if (m_Chunk != null)
                    {
                        if (age > 0)
                        {
                            // we're reading frames from the chunk but not actually doing anything with them (seeking forward)
                            // (Chunk.ReadFrames keeps an internal position pointer, so next time we call ReadFrames we should be in sync)
                            m_Chunk.ReadFrames((int)Math.Floor(age * m_Chunk.SampleFormat.MsRate));
                        }
                        else if (age < 0)
                        {
                            // the current chunk is slightly ahead, so we need to play a bit of silence until we're synced up (seeking backward)
                            int silentFrames = (int)Math.Floor(-age * m_Chunk.SampleFormat.MsRate);
                            read = silentFrames; // this makes sure we offset our output bytes so the first x frames are zeroes
                            pos = silentFrames * m_SampleFormat.FrameSize;
                        }

                        age = 0; // we don't want any frame corrections to be applied after this
                    }
                }

                int addFrames = 0; // number of frames we'll add / remove
                int everyN = 0; // interval at which we'll add / remove frames
                if (age > 0.1f)
                {
                    addFrames = (int)Math.Ceiling(age);
                }
                else if (age < -0.1f)
                {
                    addFrames = (int)Math.Floor(age);
                }

                // number of frames we'll need to actually read from the buffer
                // when we want to play faster (aka age > 0), we'll read more frames from the buffer, but skip a few in playback
                // when we want to play slower, we read less frames from the buffer and duplicate a few in playback
                int numFramesToRead = frames + addFrames - read; 

                if (addFrames != 0)
                {
                    everyN = (int)Math.Ceiling((frames + addFrames - read) / (Math.Abs(addFrames) + 1.0f));
                }

                byte[] output = new byte[bufferFrameCount * format.FrameSize]; // prepare the actual output bytes!
                while (read < numFramesToRead) // as long as we haven't read in the number of frames requested
                {
                    lock (m_Chunks)
                    {
                        // if the current chunk runs out, grab the next one from the queue
                        if (m_Chunk != null && m_Chunk.IsEndOfChunk())
                        {
                            if (m_Chunks.Count > 0)
                            {
                                m_Chunk = m_Chunks.Dequeue();
                            }
                            else // the chunk has run out, but there's no chunks waiting - return output to the audio device (this will effectively mean we've padded with zeroes, but not doing this would keep us in an infinite while loop here)
                            {
                                return output;
                            }
                        }
                    }

                    // try to read the number of frames we need, minus the number of frames we've already read
                    // (often the chunk has less frames than we're requesting, which is the reason this is inside of a while loop)
                    byte[] chunk = (m_Chunk.ReadFrames(numFramesToRead - read));
                    for (int i = 0; i < chunk.Length; i += m_SampleFormat.FrameSize) // iterate through the chunk frame by frame
                    {
                        read++;
                        Buffer.BlockCopy(chunk, i, output, pos, m_SampleFormat.FrameSize); // copy the frame from the chunk to the output buffer
                        if (everyN != 0 && read % everyN == 0) // if we reached a point where a frame needs to be added/removed
                        {
                            if (addFrames > 0)
                            {
                                // adding a frame: move back our position pointer.
                                // this means we'll effectively skip a frame from being played, while reading an extra one from the buffer instead
                                // (playing faster)
                                pos -= m_SampleFormat.FrameSize; 
                            }
                            else
                            {
                                // removing a frame: copy the current frame to the next position, and move forward our position pointer
                                // this means we'll duplicate one frame, while reading a frame less from the chunk
                                // (playing slower)
                                Buffer.BlockCopy(output, pos, output, pos + m_SampleFormat.FrameSize, m_SampleFormat.FrameSize);
                                pos += m_SampleFormat.FrameSize;
                            }
                        }
                        pos += m_SampleFormat.FrameSize;
                    }
                }
                return output;
            }

            // return silence for the requested number of frames
            return new byte[bufferFrameCount * format.FrameSize];
        }
    }
}

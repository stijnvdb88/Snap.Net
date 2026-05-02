using NAudio.Wave;

namespace Snap.Net.Broadcast
{
    public class SampleFormat : ISampleFormat
    {
        private WaveFormat m_WaveFormat;
        public SampleFormat(WaveFormat waveFormat)
        {
            m_WaveFormat = waveFormat;
        }

        public int Channels => m_WaveFormat.Channels;
        public int Rate => m_WaveFormat.SampleRate;
        public int Bits => m_WaveFormat.BitsPerSample;
    }
}
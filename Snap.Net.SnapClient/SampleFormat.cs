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

namespace Snap.Net.SnapClient
{
    public class SampleFormat
    {
        private int m_Rate;
        private int m_Channels;
        private int m_Bits;

        public int Rate => m_Rate;
        public int Bits => m_Bits;
        public int Channels => m_Channels;


        public int FrameSize { get; } = 0;
        public int SampleSize { get; } = 0;
        public double MsRate { get; } = 0;

        public SampleFormat(int rate, int channels, int bits)
        {
            m_Rate = rate;
            m_Channels = channels;
            m_Bits = bits;

            // calculating these in advance as they get requested often on the hot path
            SampleSize = m_Bits == 24 ? 4 : m_Bits / 8;
            FrameSize = m_Channels * SampleSize;
            MsRate = m_Rate / 1000.0f;
        }

    }
}

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

using System;
using System.Collections.Generic;

namespace Snap.Net.SnapClient.Time
{
    public abstract class TimeProvider : IMessageListener<TimeMessage>
    {
        private double m_Diff = 0;
        private List<double> m_DiffBuffer = new List<double>();

        public abstract double Now();

        public double NowSec()
        {
            return Now() / 1000.0f;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c2s">Client to server</param>
        /// <param name="s2c">Server to client</param>
        public void SetDiff(double c2s, double s2c)
        {
            if (Now() == 0) // this happens with audio device time providers, which return zero until we start playing
            {
                Reset();
                return;
            }

            double add = ((c2s - s2c)) / 2.0f;

            m_DiffBuffer.Add(add);
            if (m_DiffBuffer.Count > 100)
            {
                m_DiffBuffer.RemoveAt(0);
            }

            double[] sorted = m_DiffBuffer.ToArray();
            Array.Sort(sorted);

            int idx = (int)Math.Floor(sorted.Length / 2.0f);
            double newDiff = sorted[idx];
            m_Diff = newDiff;
        }

        public void OnMessageReceived(TimeMessage message)
        {
            SetDiff(message.Latency.GetMilliseconds(), Now() - message.Sent.GetMilliseconds());
        }

        public double ServerNow()
        {
            return this.ServerTime(this.Now());
        }

        public double ServerTime(double localTimeMs)
        {
            return localTimeMs + m_Diff;
        }

        public void Reset()
        {
            m_DiffBuffer.Clear();
            m_Diff = 0;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Snap.Net.SnapClient.Time
{
    class StopwatchTimeProvider : TimeProvider
    {
        private readonly Stopwatch m_Stopwatch = new Stopwatch();

        public StopwatchTimeProvider()
        {
            m_Stopwatch.Start();
        }

        public override double Now()
        {
            return m_Stopwatch.Elapsed.TotalMilliseconds;
        }
    }
}

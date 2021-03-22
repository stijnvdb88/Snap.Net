using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioToolbox;
using Foundation;
using CoreAnimation;
using Snap.Net.SnapClient;
using Snap.Net.SnapClient.Time;
using UIKit;

namespace SnapDotNet.Mobile.iOS.Player
{
    class AudioQueueTimeProvider : TimeProvider
    {
        private AudioQueue m_AudioQueue = null;
        private SampleFormat m_SampleFormat = null;

        public void Init(AudioQueue audioQueue, SampleFormat sampleFormat)
        {
            m_AudioQueue = audioQueue;
            m_SampleFormat = sampleFormat;
        }

        public override double Now()
        {
            double result = 0;
            if (m_AudioQueue == null)
            {
                return result;
            }

            AudioQueueTimeline timeline = m_AudioQueue.CreateTimeline();
            AudioTimeStamp timestamp = new AudioTimeStamp();
            bool t = false;
            m_AudioQueue.GetCurrentTime(timeline, ref timestamp, ref t);
            return timestamp.SampleTime / m_SampleFormat.Rate * 1000;
        }
    }
}
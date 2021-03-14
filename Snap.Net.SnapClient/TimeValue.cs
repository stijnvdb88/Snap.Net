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
using System.IO;

namespace Snap.Net.SnapClient
{
    [System.Serializable]
    public class TimeValue
    {
        public int sec = 0;
        public int usec = 0;

        public TimeValue(int sec, int usec)
        {
            this.sec = sec;
            this.usec = usec;
        }

        public TimeValue(byte[] buffer)
        {
            _Deserialize(buffer);
        }

        public void SetMilliseconds(double ms)
        {
            this.sec = (int)Math.Floor(ms / 1000.0f);
            this.usec = (int)((long)(ms * 1000) % 1000000);
        }

        public double GetMilliseconds()
        {
            double s = (double)this.sec * 1000;
            double us = this.usec / 1000.0f;
            return s + us;
        }

        private void _Deserialize(byte[] buffer)
        {
            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(memoryStream))
                {
                    sec = reader.ReadInt32();
                    usec = reader.ReadInt32();
                }
            }
        }

        public byte[] Serialize()
        {
            using (MemoryStream memoryStream = new MemoryStream(8))
            {
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    writer.Write(sec);
                    writer.Write(usec);
                }
                return memoryStream.GetBuffer();
            }
        }
    }
}

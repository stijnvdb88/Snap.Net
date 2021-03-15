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

using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace SnapClient.Net
{
    abstract class Device
    {
        protected Options m_Options = null;

        public abstract IEnumerable<string> List();
        public abstract IWavePlayer DeviceFactory();

        protected Device(Options options)
        {
            m_Options = options;
        }

        public bool HasValidOptions()
        {
            return m_Options.SoundCard < List().Count();
        }
    }
}

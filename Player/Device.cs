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
using System.Globalization;
using System.Linq;

namespace SnapDotNet.Player
{
    /// <summary>
    /// This class is responsible for audio device querying
    /// </summary>
    public class Device
    {
        public int Index { get; }
        public string UniqueId { get; }
        public string Name { get; }

        public Device(int index, string uniqueId, string name)
        {
            Index = index;
            UniqueId = uniqueId;
            Name = name;
        }
    }
}

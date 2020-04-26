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

namespace SnapDotNet.ControlClient.JsonRpcData
{
    public class Volume
    {
        /// <summary>
        /// OnModified event is subscribed to by SnapcastClient for replicating to server
        /// </summary>
        public event Action CLIENT_OnModified;

        public bool muted { get; set; }

        public int percent { get; set; }

        public void CLIENT_ToggleMuted()
        {
            muted = muted == false;
            CLIENT_OnModified?.Invoke();
        }

        public void CLIENT_SetPercent(int percent)
        {
            if (this.percent != percent)
            {
                this.percent = percent;
                CLIENT_OnModified?.Invoke();
            }
        }

        public override string ToString()
        {
            return string.Format("Percent: {0}, muted: {1}", percent, muted);
        }
    }
}

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
using System.Text;

namespace SnapDotNet.ControlClient.JsonRpcData
{
    public class Stream
    {
        /// <summary>
        /// OnInvalidate event is used to update UI when server data is about to be fully refreshed
        /// </summary>
        public event Action SERVER_OnInvalidate;

        /// <summary>
        /// OnStreamUpdated event is used to update UI after server reports changes
        /// </summary>
        public event Action SERVER_OnStreamUpdated;

        public string id { get; set; }
        public Meta meta { get; set; }
        public string status { get; set; }
        public Uri uri { get; set; }

        /// <summary>
        /// We use this method to update the stream member my member
        /// The server sends out a new stream object altogether, which would conflict with our event system
        /// </summary>
        /// <param name="stream"></param>
        public void SERVER_Update(Stream stream)
        {
            this.id = stream.id;
            this.meta = stream.meta;
            this.status = stream.status;
            this.uri = stream.uri;
            SERVER_OnStreamUpdated?.Invoke();
        }

        /// <summary>
        /// Called when server data is about to be fully refreshed
        /// </summary>
        public void SERVER_Invalidate()
        {
            SERVER_OnInvalidate?.Invoke();
        }
    }
}

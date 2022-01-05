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
using Snap.Net.ControlClient.JsonRpcData;

namespace SnapDotNet.ControlClient.JsonRpcData
{
    public class Stream
    {
        // https://github.com/badaix/snapcast/blob/master/doc/json_rpc_api/stream_plugin.md#pluginstreamplayercontrol
        public enum EControlCommand
        {
            play,
            pause,
            playPause,
            stop,
            next,
            previous,
            seek,
            setPosition
        }

        /// <summary>
        /// OnInvalidate event is used to update UI when server data is about to be fully refreshed
        /// </summary>
        public event Action SERVER_OnInvalidate;

        /// <summary>
        /// OnStreamUpdated event is used to update UI after server reports changes
        /// </summary>
        public event Action SERVER_OnStreamUpdated;

        /// <summary>
        /// OnStreamPropertiesUpdated event is used to update UI after server reports property (metadata) changes
        /// </summary>
        public event Action SERVER_OnStreamPropertiesUpdated;

        /// <summary>
        /// OnControlCommand: SnapcastClient listens to this for replicating to server
        /// </summary>
        public event Action<EControlCommand> CLIENT_OnControlCommand;

        public string id { get; set; }
        public Meta meta { get; set; }
        public string status { get; set; }
        public Uri uri { get; set; }
        public Properties properties { get; set; }

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

        public void SERVER_PropertiesUpdate(Properties properties)
        {
            this.properties = properties;
            SERVER_OnStreamPropertiesUpdated?.Invoke();
        }

        /// <summary>
        /// Called when server data is about to be fully refreshed
        /// </summary>
        public void SERVER_Invalidate()
        {
            SERVER_OnInvalidate?.Invoke();
        }

        /// <summary>
        /// Called by client when sending control command to stream (play/pause/next/previous/etc)
        /// </summary>
        /// <param name="command"></param>
        public void CLIENT_SendControlCommand(EControlCommand command)
        {
            CLIENT_OnControlCommand?.Invoke(command);
        }
    }
}

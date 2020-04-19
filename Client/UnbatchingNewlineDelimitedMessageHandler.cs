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
#nullable enable
namespace SnapDotNet.Client
{
    using Microsoft;
    using Nerdbank.Streams;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using StreamJsonRpc;
    using StreamJsonRpc.Protocol;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Pipelines;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This is a specialized NewLineDelimited message handler that supports *receiving* JSON-RPC messages batched in an array.
    /// </summary>
    public class UnbatchingNewLineDelimitedMessageHandler : NewLineDelimitedMessageHandler
    {
        private static readonly Encoding Encoding = Encoding.UTF8;
        private static readonly byte JsonArrayFirstByte = Encoding.GetBytes("[").Single();
        private readonly Queue<JsonRpcMessage> m_PendingMessages = new Queue<JsonRpcMessage>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UnbatchingNewLineDelimitedMessageHandler"/> class
        /// that uses the <see cref="JsonMessageFormatter"/> to serialize messages as textual JSON.
        /// </summary>
        /// <inheritdoc cref="NewLineDelimitedMessageHandler(Stream, Stream)"/>
        public UnbatchingNewLineDelimitedMessageHandler(Stream read, Stream write)
            : base(read, write, new JsonMessageFormatter())
        {
        }


        protected override async ValueTask<JsonRpcMessage?> ReadCoreAsync(CancellationToken cancellationToken)
        {
            Assumes.NotNull(this.Reader);
            if (this.m_PendingMessages.Count > 0)
            {
                return this.m_PendingMessages.Dequeue();
            }
            while (true)
            {
                ReadResult readResult = await this.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                if (readResult.Buffer.Length == 0 && readResult.IsCompleted)
                {
                    return default; // remote end disconnected at a reasonable place.
                }

                SequencePosition? lf = readResult.Buffer.PositionOf((byte)'\n');
                if (!lf.HasValue)
                {
                    if (readResult.IsCompleted)
                    {
                        throw new EndOfStreamException();
                    }

                    // Indicate that we can't find what we're looking for and read again.
                    this.Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
                    continue;
                }

                ReadOnlySequence<byte> line = readResult.Buffer.Slice(0, lf.Value);

                // If the line ends with an \r (that precedes the \n we already found), trim that as well.
                SequencePosition? cr = line.PositionOf((byte)'\r');
                if (cr.HasValue && line.GetPosition(1, cr.Value).Equals(lf))
                {
                    line = line.Slice(0, line.Length - 1);
                }

                try
                {
                    // Skip over blank lines.
                    if (line.Length > 0)
                    {
                        if (line.First.Span[0] == JsonArrayFirstByte)
                        {
                            // This is a JSON array of JSON-RPC messages.
                            // The formatter doesn't support this, so break them up first.
                            using (var reader = new JsonTextReader(new SequenceTextReader(line, Encoding)))
                            {
                                var array = JArray.Load(reader);
                                if (array.Count == 0)
                                {
                                    // An empty array? I hope the server never sends that.
                                    throw new NotSupportedException("An empty JSON array was received.");
                                }

                                foreach (JToken messageJson in array.Children())
                                {
                                    byte[] encodedMessage = Encoding.GetBytes(messageJson.ToString());
                                    this.m_PendingMessages.Enqueue(this.Formatter.Deserialize(new ReadOnlySequence<byte>(encodedMessage)));
                                }
                            }

                            return this.m_PendingMessages.Dequeue();
                        }
                        return this.Formatter.Deserialize(line);
                    }
                }
                finally
                {
                    // Advance to the next line.
                    this.Reader.AdvanceTo(readResult.Buffer.GetPosition(1, lf.Value));
                }
            }
        }
        /// <inheritdoc />    
    }
}
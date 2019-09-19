// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions.Utilities;
using Newtonsoft.Json;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal class ReceiveResponseAssembler : IAssembler
    {
        private readonly Func<Guid, ReceiveResponse, Task> _onCompleted;
        private readonly IStreamManager _streamManager;
        private readonly int? _length;
        private object _syncLock = new object();

        public ReceiveResponseAssembler(Header header, IStreamManager streamManager, Func<Guid, ReceiveResponse, Task> onCompleted)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            _streamManager = streamManager ?? new StreamManager();
            _onCompleted = onCompleted ?? throw new ArgumentNullException(nameof(onCompleted));
            Id = header.Id;
            _length = header.End ? (int?)header.PayloadLength : null;
        }

        public Guid Id { get; private set; }

        public bool End { get; private set; }

        protected static JsonSerializer Serializer { get; set; } = JsonSerializer.Create(SerializationSettings.DefaultSerializationSettings);

        private Stream Stream { get; set; }

        public Stream CreateStreamFromPayload()
        {
            if (_length.HasValue)
            {
                return new MemoryStream(_length.Value);
            }
            else
            {
                return new MemoryStream();
            }
        }

        public Stream GetPayloadAsStream()
        {
            lock (_syncLock)
            {
                if (Stream == null)
                {
                    Stream = CreateStreamFromPayload();
                }
            }

            return Stream;
        }

        public void OnReceive(Header header, Stream stream, int contentLength)
        {
            if (header.End)
            {
                End = header.End;

                // Move stream back to the beginning for reading
                stream.Position = 0;

                // Execute the request on a seperate Task
                Background.Run(() => ProcessResponse(stream));
            }

            // else: still receiving data into the stream
        }

        public void Close() => _streamManager.CloseStream(Id);

        private async Task ProcessResponse(Stream stream)
        {
            using (var textReader = new StreamReader(stream))
            {
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    var responsePayload = Serializer.Deserialize<ResponsePayload>(jsonReader);

                    var response = new ReceiveResponse()
                    {
                        StatusCode = responsePayload.StatusCode,
                        Streams = new List<IContentStream>(),
                    };

                    if (responsePayload.Streams != null)
                    {
                        foreach (var streamDescription in responsePayload.Streams)
                        {
                            if (!Guid.TryParse(streamDescription.Id, out var id))
                            {
                                throw new InvalidDataException($"Stream description id '{streamDescription.Id}' is not a Guid");
                            }

                            var streamAssembler = _streamManager.GetPayloadAssembler(id);
                            streamAssembler.ContentType = streamDescription.ContentType;
                            streamAssembler.ContentLength = streamDescription.Length;

                            response.Streams.Add(new ContentStream(id, streamAssembler)
                            {
                                Length = streamDescription.Length,
                                ContentType = streamDescription.ContentType,
                            });
                        }
                    }

                    await _onCompleted(this.Id, response).ConfigureAwait(false);
                }
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal class PayloadStreamAssembler : IAssembler
    {
        private object _syncLock = new object();
        private readonly IStreamManager _streamManager;

        public PayloadStreamAssembler(IStreamManager streamManager, Guid id)
        {
            _streamManager = streamManager ?? new StreamManager();
            Id = id;
        }

        public PayloadStreamAssembler(IStreamManager streamManager, Guid id, string type, int? length)
            : this(streamManager, id)
        {
            ContentType = type;
            ContentLength = length;
        }

        public int? ContentLength { get; set; }

        public string ContentType { get; set; } = string.Empty;

        public Guid Id { get; private set; }

        public bool End { get; private set; }

        protected static JsonSerializer Serializer { get; set; } = JsonSerializer.Create(SerializationSettings.DefaultSerializationSettings);

        private Stream Stream { get; set; }

        public Stream CreateStreamFromPayload() => new PayloadStream(this);

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
                End = true;
                ((PayloadStream)stream).DoneProducing();
            }
        }

        public void Close() => _streamManager.CloseStream(Id);
    }
}

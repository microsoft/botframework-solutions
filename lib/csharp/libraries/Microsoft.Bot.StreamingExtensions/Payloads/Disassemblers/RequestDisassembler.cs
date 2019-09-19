// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions.PayloadTransport;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal class RequestDisassembler : PayloadDisassembler
    {
        public RequestDisassembler(IPayloadSender sender, Guid id, StreamingRequest request)
            : base(sender, id)
        {
            Request = request;
        }

        public StreamingRequest Request { get; private set; }

        public override char Type => PayloadTypes.Request;

        public override Task<StreamWrapper> GetStream()
        {
            var payload = new RequestPayload()
            {
                Verb = Request.Verb,
                Path = Request.Path,
            };

            if (Request.Streams != null)
            {
                payload.Streams = new List<StreamDescription>();
                foreach (var contentStream in Request.Streams)
                {
                    var description = GetStreamDescription(contentStream);

                    payload.Streams.Add(description);
                }
            }

            Serialize(payload, out MemoryStream memoryStream, out int streamLength);

            return Task.FromResult(new StreamWrapper()
            {
                Stream = memoryStream,
                StreamLength = streamLength,
            });
        }
    }
}

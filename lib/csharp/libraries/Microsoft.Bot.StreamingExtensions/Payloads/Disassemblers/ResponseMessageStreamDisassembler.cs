// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions.PayloadTransport;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal class ResponseMessageStreamDisassembler : PayloadDisassembler
    {
        public ResponseMessageStreamDisassembler(IPayloadSender sender, ResponseMessageStream contentStream)
            : base(sender, contentStream.Id)
        {
            ContentStream = contentStream;
        }

        public ResponseMessageStream ContentStream { get; private set; }

        public override char Type => PayloadTypes.Stream;

        public override async Task<StreamWrapper> GetStream()
        {
            var stream = await ContentStream.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var description = GetStreamDescription(ContentStream);

            return new StreamWrapper()
            {
                Stream = stream,
                StreamLength = description.Length,
            };
        }
    }
}

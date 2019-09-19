// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions.PayloadTransport;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal class CancelDisassembler
    {
        public CancelDisassembler(IPayloadSender sender, Guid id, char type)
        {
            Sender = sender;
            Id = id;
            Type = type;
        }

        private IPayloadSender Sender { get; set; }

        private Guid Id { get; set; }

        private char Type { get; set; }

        public Task Disassemble()
        {
            var header = new Header()
            {
                Type = Type,
                Id = Id,
                PayloadLength = 0,
                End = true,
            };

            Sender.SendPayload(header, null, true, null);

            return Task.CompletedTask;
        }
    }
}

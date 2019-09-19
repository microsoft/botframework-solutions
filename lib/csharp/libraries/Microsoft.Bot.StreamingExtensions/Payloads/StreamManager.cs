// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.IO;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal class StreamManager : IStreamManager
    {
        private readonly ConcurrentDictionary<Guid, PayloadStreamAssembler> _activeAssemblers;
        private readonly Action<PayloadStreamAssembler> _onCancelStream;

        public StreamManager(Action<PayloadStreamAssembler> onCancelStream = null)
        {
            // If no callback is defined, make it a noop to avoid null checking everywhere.
            _onCancelStream = onCancelStream ?? ((a) => { });
            _activeAssemblers = new ConcurrentDictionary<Guid, PayloadStreamAssembler>();
        }

        public PayloadStreamAssembler GetPayloadAssembler(Guid id)
        {
            if (!_activeAssemblers.TryGetValue(id, out var assembler))
            {
                // a new id has come in, start a new task to process it
                assembler = new PayloadStreamAssembler(this, id);
                if (!_activeAssemblers.TryAdd(id, assembler))
                {
                    // Don't need to dispose the assembler because it was never used
                    // Get the one that is in use
                    _activeAssemblers.TryGetValue(id, out assembler);
                }
            }

            return assembler;
        }

        public Stream GetPayloadStream(Header header)
        {
            var assembler = GetPayloadAssembler(header.Id);

            return assembler.GetPayloadAsStream();
        }

        public void OnReceive(Header header, Stream contentStream, int contentLength)
        {
            if (_activeAssemblers.TryGetValue(header.Id, out var assembler))
            {
                assembler.OnReceive(header, contentStream, contentLength);
            }
        }

        public void CloseStream(Guid id)
        {
            if (_activeAssemblers.TryRemove(id, out var assembler))
            {
                // decide whether to cancel it or not
                var stream = assembler.GetPayloadAsStream();
                if ((assembler.ContentLength.HasValue && stream.Length < assembler.ContentLength.Value) ||
                    !assembler.End)
                {
                    _onCancelStream(assembler);
                }
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal class ContentStream : IContentStream
    {
        private readonly PayloadStreamAssembler _assembler;

        internal ContentStream(Guid id, PayloadStreamAssembler assembler)
        {
            Id = id;
            _assembler = assembler ?? throw new ArgumentNullException();
            Stream = _assembler.GetPayloadAsStream();
        }

        public Guid Id { get; private set; }

        public string ContentType { get; set; }

        public int? Length { get; set; }

        public Stream Stream { get; private set; }

        public void Cancel() => _assembler.Close();
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal interface IStreamManager
    {
        PayloadStreamAssembler GetPayloadAssembler(Guid id);

        Stream GetPayloadStream(Header header);

        void OnReceive(Header header, Stream contentStream, int contentLength);

        void CloseStream(Guid id);
    }
}

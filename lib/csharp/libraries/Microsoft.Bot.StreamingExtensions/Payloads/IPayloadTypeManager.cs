// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal interface IPayloadTypeManager
    {
        IAssembler CreatePayloadAssembler(Header header);
    }
}

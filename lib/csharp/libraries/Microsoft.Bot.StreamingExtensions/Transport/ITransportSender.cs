// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Bot.StreamingExtensions.Transport
{
#if DEBUG
    public
#else
    internal
#endif
    interface ITransportSender : ITransport
    {
        Task<int> SendAsync(byte[] buffer, int offset, int count);
    }
}

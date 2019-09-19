// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.StreamingExtensions.Transport
{
#if DEBUG
    public
#else
    internal
#endif
    interface ITransport : IDisposable
    {
        bool IsConnected { get; }

        void Close();
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.StreamingExtensions.PayloadTransport
{
    internal class TransportDisconnectedException : Exception
    {
        public TransportDisconnectedException()
            : base()
        {
        }

        public TransportDisconnectedException(string reason)
            : base()
        {
            Reason = reason;
        }

        public string Reason { get; set; }
    }
}

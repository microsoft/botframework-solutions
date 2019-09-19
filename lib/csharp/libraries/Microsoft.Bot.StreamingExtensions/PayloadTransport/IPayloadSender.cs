// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions.Payloads;
using Microsoft.Bot.StreamingExtensions.Transport;

namespace Microsoft.Bot.StreamingExtensions.PayloadTransport
{
    internal interface IPayloadSender
    {
        event DisconnectedEventHandler Disconnected;

        bool IsConnected { get; }

        void Connect(ITransportSender sender);

        void SendPayload(Header header, Stream payload, bool isLengthKnown, Func<Header, Task> sentCallback);

        void Disconnect(DisconnectedEventArgs e = null);
    }
}

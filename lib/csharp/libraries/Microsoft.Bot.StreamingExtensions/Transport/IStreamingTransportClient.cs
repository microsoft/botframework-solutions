// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.StreamingExtensions.Transport
{
    /// <summary>
    /// Implemented by clients compatible with the Bot Framework Protocol 3 with Streaming Extensions.
    /// </summary>
    public interface IStreamingTransportClient : IDisposable
    {
        /// <summary>
        /// An event used to signal when the underlying connection has disconnected.
        /// </summary>
        event DisconnectedEventHandler Disconnected;

        /// <summary>
        /// Gets a value indicating whether this client is currently connected.
        /// </summary>
        /// <value>
        /// True if this client is currently connected, otherwise false.
        /// </value>
        bool IsConnected { get; }

        /// <summary>
        /// The task used to establish a connection for this client.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ConnectAsync();

        /// <summary>
        /// Establish a connection passing along additional headers.
        /// </summary>
        /// <param name="requestHeaders">Dictionary of header name and header value to be passed during connection. Generally, you will need channelID and Authorization.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ConnectAsync(IDictionary<string, string> requestHeaders);

        /// <summary>
        /// Task used to send data over this client connection.
        /// </summary>
        /// <param name="message">The <see cref="StreamingRequest"/> to send.</param>
        /// <param name="cancellationToken">An optional <see cref="CancellationToken"/> used to signal this operation should be cancelled.</param>
        /// <returns>A <see cref="Task"/> of type <see cref="ReceiveResponse"/> handling the send operation.</returns>
        Task<ReceiveResponse> SendAsync(StreamingRequest message, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Method used to disconnect this client.
        /// </summary>
        void Disconnect();
    }
}

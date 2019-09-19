// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.StreamingExtensions.Transport
{
    /// <summary>
    /// Implemented by servers compatible with the Bot Framework Protocol 3 with Streaming Extensions.
    /// </summary>
    public interface IStreamingTransportServer
    {
        /// <summary>
        /// An event used to signal when the underlying connection has disconnected.
        /// </summary>
        event DisconnectedEventHandler Disconnected;

        /// <summary>
        /// Used to establish the connection used by this server and begin listening for incoming messages.
        /// </summary>
        /// <returns>A <see cref="Task"/> to handle the server listen operation.</returns>
        Task StartAsync();

        /// <summary>
        /// Task used to send data over this server connection.
        /// </summary>
        /// <param name="request">The <see cref="StreamingRequest"/> to send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to signal this operation should be cancelled.</param>
        /// <returns>A <see cref="Task"/> of type <see cref="ReceiveResponse"/> handling the send operation.</returns>
        Task<ReceiveResponse> SendAsync(StreamingRequest request, CancellationToken cancellationToken = default(CancellationToken));
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.StreamingExtensions
{
    /// <summary>
    /// Implemented by classes used to process incoming requests sent over an IStreamingTransport and adhering to the Bot Framework Protocol v3 with Streaming Extensions.
    /// </summary>
    public abstract class RequestHandler
    {
    #pragma warning disable IDE0034
        /// <summary>
        /// The method that must be implemented in order to handle incoming requests.
        /// </summary>
        /// <param name="request">A <see cref="ReceiveRequest"/> for this handler to process.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="context">Optional context to process the request within.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> that will produce a <see cref="StreamingResponse"/> on successful completion.</returns>
        public abstract Task<StreamingResponse> ProcessRequestAsync(ReceiveRequest request, ILogger<RequestHandler> logger, object context = null, CancellationToken cancellationToken = default(CancellationToken));
    #pragma warning restore IDE0034
    }
}

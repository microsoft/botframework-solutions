// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.StreamingExtensions.Payloads;
using Microsoft.Bot.StreamingExtensions.Transport;

namespace Microsoft.Bot.StreamingExtensions
{
    /// <summary>
    /// A response received by a <see cref="IStreamingTransportServer"/> or <see cref="IStreamingTransportClient"/>.
    /// </summary>
    public class ReceiveResponse
    {
        /// <summary>
        /// Gets or sets the status code of this response.
        /// </summary>
        /// <value>
        /// The numeric portion of a status code.
        /// </value>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the collection of <see cref="IContentStream"/>s contained within this response.
        /// </summary>
        /// <value>
        /// A <see cref="List{T}"/> of type <see cref="IContentStream"/> containing information on streams attached to this response.
        /// </value>
        public List<IContentStream> Streams { get; set; } = new List<IContentStream>();
    }
}

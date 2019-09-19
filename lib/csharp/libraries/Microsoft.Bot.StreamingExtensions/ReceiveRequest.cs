// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.StreamingExtensions.Payloads;

namespace Microsoft.Bot.StreamingExtensions
{
    /// <summary>
    /// An incoming request from a remote client.
    /// </summary>
    public class ReceiveRequest
    {
        /// <summary>
        /// Gets or sets the verb action this request wants to perform.
        /// </summary>
        /// <value>
        /// The string representation of an HTTP verb.
        /// </value>
        public string Verb { get; set; }

        /// <summary>
        /// Gets or sets the path this request wants to be routed to.
        /// </summary>
        /// <value>
        /// The string representation of the URL style path to request wants to be routed to.
        /// </value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the collection of stream attachments included in this request.
        /// </summary>
        /// <value>
        /// A <see cref="List{T}"/> of <see cref="IContentStream"/> items associated with this request.
        /// </value>
        public List<IContentStream> Streams { get; set; } = new List<IContentStream>();
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    /// <summary>
    /// An attachment contained within a <see cref="StreamingRequest"/>'s stream collection,
    /// which itself contains any form of media item.
    /// </summary>
    public class ResponseMessageStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseMessageStream"/> class.
        /// and assigns an unique guid as its Id.
        /// </summary>
        public ResponseMessageStream()
            : this(Guid.NewGuid())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseMessageStream"/> class.
        /// </summary>
        /// <param name="id">A <see cref="Guid"/> to assign as the Id of this instance of <see cref="ResponseMessageStream"/>.
        /// If null a new <see cref="Guid"/> will be generated.
        /// </param>
        public ResponseMessageStream(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// Gets the <see cref="Guid"/> used to identify this <see cref="ResponseMessageStream"/>.
        /// </summary>
        /// <value>
        /// A <see cref="Guid"/> used to identify this <see cref="ResponseMessageStream"/>.
        /// </value>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="HttpContent"/> of this <see cref="ResponseMessageStream"/>.
        /// </summary>
        /// <value>
        /// The <see cref="HttpContent"/> of this <see cref="ResponseMessageStream"/>.
        /// </value>
        public HttpContent Content { get; set; }
    }
}

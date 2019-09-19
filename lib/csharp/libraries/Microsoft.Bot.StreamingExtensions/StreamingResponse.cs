// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.Bot.StreamingExtensions.Payloads;

namespace Microsoft.Bot.StreamingExtensions
{
    /// <summary>
    /// Implementation split between Response and ResponseEx.
    /// The basic response type sent over Bot Framework Protocol 3 with Streaming Extensions transports,
    /// equivalent to HTTP response messages.
    /// </summary>
    public class StreamingResponse
    {
        /// <summary>
        /// Gets or sets the numeric status code for the response.
        /// </summary>
        /// <value>
        /// The numeric status code for the response.
        /// </value>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the collection of streams attached to this response.
        /// </summary>
        /// <value>
        /// A <see cref="List{T}"/> of type <see cref="ResponseMessageStream"/>.
        /// </value>
        public List<ResponseMessageStream> Streams { get; set; }

        /// <summary>
        /// Creates a response indicating the requested resource was not found.
        /// </summary>
        /// <param name="body">An optional body containing additional information.</param>
        /// <returns>A response with the appropriate statuscode and passed in body.</returns>
        public static StreamingResponse NotFound(HttpContent body = null) => CreateResponse(HttpStatusCode.NotFound, body);

        /// <summary>
        /// Creates a response indicating the requested resource is forbidden.
        /// </summary>
        /// <param name="body">An optional body containing additional information.</param>
        /// <returns>A response with the appropriate statuscode and passed in body.</returns>
        public static StreamingResponse Forbidden(HttpContent body = null) => CreateResponse(HttpStatusCode.Forbidden, body);

        /// <summary>
        /// Creates a response indicating the request was successful.
        /// </summary>
        /// <param name="body">An optional body containing additional information.</param>
        /// <returns>A response with the appropriate statuscode and passed in body.</returns>
        public static StreamingResponse OK(HttpContent body = null) => CreateResponse(HttpStatusCode.OK, body);

        /// <summary>
        /// Creates a response indicating the server encountered an error while processing the request.
        /// </summary>
        /// <param name="body">An optional body containing additional information.</param>
        /// <returns>A response with the appropriate statuscode and passed in body.</returns>
        public static StreamingResponse InternalServerError(HttpContent body = null) => CreateResponse(HttpStatusCode.InternalServerError, body);

        /// <summary>
        /// Creates a response using the passed in statusCode and optional body.
        /// </summary>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> to set on the <see cref="StreamingResponse"/>.</param>
        /// <param name="body">An optional body containing additional information.</param>
        /// <returns>A response with the appropriate statuscode and passed in body.</returns>
        public static StreamingResponse CreateResponse(HttpStatusCode statusCode, HttpContent body = null)
        {
            var response = new StreamingResponse()
            {
                StatusCode = (int)statusCode,
            };

            if (body != null)
            {
                response.AddStream(body);
            }

            return response;
        }

        /// <summary>
        /// Adds a new stream to the passed in <see cref="StreamingResponse"/> containing the passed in content.
        /// Throws <see cref="ArgumentNullException"/> if content is null.
        /// </summary>
        /// <param name="content">An <see cref="HttpContent"/> instance containing the data to insert into the stream.</param>
        public void AddStream(HttpContent content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (Streams == null)
            {
                Streams = new List<ResponseMessageStream>();
            }

            Streams.Add(
                new ResponseMessageStream()
                {
                    Content = content,
                });
        }
    }
}

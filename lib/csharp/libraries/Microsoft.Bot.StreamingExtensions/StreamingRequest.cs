// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Bot.StreamingExtensions.Payloads;

namespace Microsoft.Bot.StreamingExtensions
{
    /// <summary>
    /// Implementation split between Request and RequestEx.
    /// The basic request type sent over Bot Framework Protocol 3 with Streaming Extensions transports,
    /// equivalent to HTTP request messages.
    /// </summary>
    public class StreamingRequest
    {
        /// <summary>
        /// Verb used by requests to get resources hosted on a remote server.
        /// </summary>
        public const string GET = "GET";

        /// <summary>
        /// Verb used by requests posting data to a remote server.
        /// </summary>
        public const string POST = "POST";

        /// <summary>
        /// Verb used by requests putting updated data on a remote server.
        /// </summary>
        public const string PUT = "PUT";

        /// <summary>
        /// Verb used by requests to delete data hosted on a remote server.
        /// </summary>
        public const string DELETE = "DELETE";

        /// <summary>
        /// Gets or sets the verb action this request will perform.
        /// </summary>
        /// <value>
        /// The string representation of an HTTP verb.
        /// </value>
        public string Verb { get; set; }

        /// <summary>
        /// Gets or sets the path this request will route to on the remote server.
        /// </summary>
        /// <value>
        /// The string representation of the URL style path to request at the remote server.
        /// </value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the collection of stream attachments included in this request.
        /// </summary>
        /// <value>
        /// A <see cref="List{T}"/> of <see cref="ResponseMessageStream"/> items associated with this request.
        /// </value>
        public List<ResponseMessageStream> Streams { get; set; }

        /// <summary>
        /// Creates a <see cref="StreamingRequest"/> to get resources hosted on a remote server.
        /// </summary>
        /// <param name="path">Optional path where the resource can be found on the remote server.</param>
        /// <param name="body">Optional body to send to the remote server.</param>
        /// <returns>A <see cref="StreamingRequest"/> with appropriate status code and body.</returns>
        public static StreamingRequest CreateGet(string path = null, HttpContent body = null) => CreateRequest(GET, path, body);

        /// <summary>
        /// Creates a <see cref="StreamingRequest"/> to post data to a remote server.
        /// </summary>
        /// <param name="path">Optional path where the resource can be found on the remote server.</param>
        /// <param name="body">Optional body to send to the remote server.</param>
        /// <returns>A <see cref="StreamingRequest"/> with appropriate status code and body.</returns>
        public static StreamingRequest CreatePost(string path = null, HttpContent body = null) => CreateRequest(POST, path, body);

        /// <summary>
        /// Creates a <see cref="StreamingRequest"/> to put updated data on a remote server.
        /// </summary>
        /// <param name="path">Optional path where the resource can be found on the remote server.</param>
        /// <param name="body">Optional body to send to the remote server.</param>
        /// <returns>A <see cref="StreamingRequest"/> with appropriate status code and body.</returns>
        public static StreamingRequest CreatePut(string path = null, HttpContent body = null) => CreateRequest(PUT, path, body);

        /// <summary>
        /// Creates a <see cref="StreamingRequest"/> to delete data hosted on a remote server.
        /// </summary>
        /// <param name="path">Optional path where the resource can be found on the remote server.</param>
        /// <param name="body">Optional body to send to the remote server.</param>
        /// <returns>A <see cref="StreamingRequest"/> with appropriate status code and body.</returns>
        public static StreamingRequest CreateDelete(string path = null, HttpContent body = null) => CreateRequest(DELETE, path, body);

        /// <summary>
        /// Creates a <see cref="StreamingRequest"/> with the passed in method, path, and body.
        /// </summary>
        /// <param name="method">The HTTP verb to use for this request.</param>
        /// <param name="path">Optional path where the resource can be found on the remote server.</param>
        /// <param name="body">Optional body to send to the remote server.</param>
        /// <returns>On success returns a <see cref="StreamingRequest"/> with appropriate status code and body, otherwise returns null.</returns>
        public static StreamingRequest CreateRequest(string method, string path = null, HttpContent body = null)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                return null;
            }

            var request = new StreamingRequest()
            {
                Verb = method,
                Path = path,
            };

            if (body != null)
            {
                request.AddStream(body);
            }

            return request;
        }

        /// <summary>
        /// Adds a new stream attachment to this <see cref="StreamingRequest"/>.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> to include in the new stream attachment.</param>
        public void AddStream(HttpContent content) => AddStream(content, Guid.NewGuid());

        /// <summary>
        /// Adds a new stream attachment to this <see cref="StreamingRequest"/>.
        /// </summary>
        /// <param name="content">The <see cref="HttpContent"/> to include in the new stream attachment.</param>
        /// <param name="streamId">The id to assign to this stream attachment.</param>
        public void AddStream(HttpContent content, Guid streamId)
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
                new ResponseMessageStream(streamId)
                {
                    Content = content,
                });
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Bot.StreamingExtensions
{
    /// <summary>
    /// Helper methods for use with instances of the <see cref="StreamingRequest"/> class.
    /// </summary>
    public static class StreamingRequestExtensions
    {
        /// <summary>
        /// Adds a new stream to this <see cref="StreamingRequest"/> containing the passed in body.
        /// Noop on null body or null request.
        /// </summary>
        /// <param name="request">The <see cref="StreamingRequest"/> instance to attach this body to.</param>
        /// <param name="body">A string containing the data to insert into the stream.</param>
        public static void SetBody(this StreamingRequest request, string body)
        {
            if (request == null || string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            request.AddStream(new StringContent(body, Encoding.UTF8));
        }

        /// <summary>
        /// Adds a new stream to this <see cref="StreamingRequest"/> containing the passed in body.
        /// Noop on null body or null request.
        /// </summary>
        /// <param name="request">The <see cref="StreamingRequest"/> instance to attach this body to.</param>
        /// <param name="body">An object containing the data to insert into the stream.</param>
        public static void SetBody(this StreamingRequest request, object body)
        {
            if (request == null || body == null)
            {
                return;
            }

            var json = JsonConvert.SerializeObject(body, SerializationSettings.BotSchemaSerializationSettings);
            request.AddStream(new StringContent(json, Encoding.UTF8, SerializationSettings.ApplicationJson));
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Bot.StreamingExtensions
{
    /// <summary>
    /// Extends the <see cref="StreamingResponse"/> class with additional methods.
    /// </summary>
    public static class StreamingResponseExtensions
    {
        /// <summary>
        /// Adds a new stream to this <see cref="StreamingResponse"/> containing the passed in body.
        /// Noop on empty body or null response.
        /// </summary>
        /// <param name="response">The <see cref="StreamingResponse"/> instance to attach this body to.</param>
        /// <param name="body">A string containing the data to insert into the stream.</param>
        public static void SetBody(this StreamingResponse response, string body)
        {
            if (response == null || string.IsNullOrWhiteSpace(body))
            {
                return;
            }

            response.AddStream(new StringContent(body, Encoding.UTF8));
        }

        /// <summary>
        /// Adds a new stream to this <see cref="StreamingResponse"/> containing the passed in body.
        /// Noop on null body or null response.
        /// </summary>
        /// <param name="response">The <see cref="StreamingResponse"/> instance to attach this body to.</param>
        /// <param name="body">An object containing the data to insert into the stream.</param>
        public static void SetBody(this StreamingResponse response, object body)
        {
            if (response == null || body == null)
            {
                return;
            }

            var json = JsonConvert.SerializeObject(body, SerializationSettings.BotSchemaSerializationSettings);
            response.AddStream(new StringContent(json, Encoding.UTF8, SerializationSettings.ApplicationJson));
        }
    }
}

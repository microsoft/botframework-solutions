// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal class RequestPayload
    {
#pragma warning disable SA1609
        /// <summary>
        /// Gets or sets request verb, null on responses.
        /// </summary>
        [JsonProperty("verb")]
        public string Verb { get; set; }

        /// <summary>
        /// Gets or sets request path; null on responses.
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets assoicated stream descriptions.
        /// </summary>
        [JsonProperty("streams")]
        public List<StreamDescription> Streams { get; set; }
#pragma warning restore SA1609
    }
}

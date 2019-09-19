// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Bot.StreamingExtensions.Payloads
{
    internal class StreamDescription
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string ContentType { get; set; }

        [JsonProperty("length")]
        public int? Length { get; set; }
    }
}

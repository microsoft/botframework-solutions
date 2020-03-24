// Copyright(c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace DirectLine.Web.Models
{
    [JsonObject]
    public class DirectLineResponse
    {
        [JsonProperty("conversationId")]
        public string ConversationId { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}

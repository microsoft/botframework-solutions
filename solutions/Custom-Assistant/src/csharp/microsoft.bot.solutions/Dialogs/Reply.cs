// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Dialogs
{
    using Newtonsoft.Json;

    public class Reply
    {
        [JsonProperty("cardText")]
        public string CardText { get; set; }

        [JsonProperty("speak")]
        public string Speak { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}

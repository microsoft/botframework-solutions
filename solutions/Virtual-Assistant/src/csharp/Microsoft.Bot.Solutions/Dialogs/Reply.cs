// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Dialogs
{
    using Newtonsoft.Json;

    public class Reply
    {
        [JsonProperty("cardText")]
        public string CardText { get; set; }

        [JsonProperty("speak")]
        public string Speak { get; set; }

        /// <summary>
        /// Gets or sets the  <see cref="Activity.Text"/> property of an <see cref="Activity"/>.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}

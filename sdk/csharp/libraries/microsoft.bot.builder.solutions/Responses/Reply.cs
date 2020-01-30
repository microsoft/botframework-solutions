// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Responses
{
    using System;
    using Newtonsoft.Json;

    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class Reply
    {
        [JsonProperty("cardText")]
        public string CardText { get; set; }

        [JsonProperty("speak")]
        public string Speak { get; set; }

        /// <summary>
        /// Gets or sets the Reply Text.
        /// </summary>
        /// <value>
        /// The  <see cref="Activity.Text"/> property of an <see cref="Activity"/>.
        /// </value>
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
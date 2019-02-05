// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Dialogs
{
    using System;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    public class BotResponse
    {
        private string inputHint;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotResponse"/> class.
        /// </summary>
        public BotResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BotResponse"/> class.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="speak"></param>
        /// <param name="inputHint"></param>
        public BotResponse(string text, string speak, string inputHint = InputHints.AcceptingInput)
        {
            this.Replies = new Reply[1];
            this.Replies[0] = new Reply
            {
                Text = text,
                Speak = speak,
            };

            this.InputHint = inputHint;
        }

        [JsonProperty("replies")]
        public Reply[] Replies { get; set; }

        [JsonProperty("suggestedActions")]
        public string[] SuggestedActions { get; set; }

        [JsonProperty("inputHint")]
        public string InputHint
        {
            get => this.inputHint ?? (this.inputHint = InputHints.AcceptingInput);
            set => this.inputHint = value;
        }

        [JsonIgnore]
        public Reply Reply => this.Replies.Length > 0 ? this.Replies[this.GetRandom(this.Replies.Length)] : null;

        private int GetRandom(int upper)
        {
            var rnd = new Random();
            return rnd.Next(0, upper);
        }
    }
}

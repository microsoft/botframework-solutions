﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Responses
{
    using System;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    public class ResponseTemplate
    {
        private string inputHint;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseTemplate"/> class.
        /// </summary>
        public ResponseTemplate()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseTemplate"/> class.
        /// </summary>
        /// <param name="text">Response Text.</param>
        /// <param name="speak">Response Speak Variant.</param>
        /// <param name="inputHint">Input Hint.</param>
        public ResponseTemplate(string text, string speak, string inputHint = InputHints.AcceptingInput)
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

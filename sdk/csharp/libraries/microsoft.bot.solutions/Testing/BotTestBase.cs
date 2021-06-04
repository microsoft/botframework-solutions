// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Bot.Solutions.Responses;

namespace Microsoft.Bot.Solutions.Testing
{
    [Obsolete("This class is being deprecated. Please use Microsoft.Bot.Solutions.Responses.LocaleTemplateManager to handle response generation.")]
    [ExcludeFromCodeCoverageAttribute]
    public abstract class BotTestBase
    {
        private static readonly Regex ResponseTokensRegex = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);

        public ResponseManager ResponseManager { get; set; }

        public virtual void Initialize()
        {
        }

        protected string[] ParseReplies(string templateId, string[] tokens)
        {
            var replies = ResponseManager.GetResponseTemplate(templateId).Replies;
            var responses = new string[replies.Length];
            for (var i = 0; i < replies.Length; i++)
            {
                var tokenIndex = i;
                var replacedString = ResponseTokensRegex.Replace(replies[i].Text, match => tokens[tokenIndex]);
                responses[i] = replacedString;
            }

            return responses;
        }

        protected string[] ParseReplies(string templateId, StringDictionary tokens = null)
        {
            var replies = ResponseManager.GetResponseTemplate(templateId).Replies;
            var responses = new string[replies.Length];

            for (var i = 0; i < replies.Length; i++)
            {
                responses[i] = tokens == null ? replies[i].Text : this.ResponseManager.Format(replies[i].Text, tokens);
            }

            return responses;
        }

        protected string[] ParseRepliesSpeak(string templateId, StringDictionary tokens = null)
        {
            var replies = ResponseManager.GetResponseTemplate(templateId).Replies;
            var responses = new string[replies.Length];

            for (var i = 0; i < replies.Length; i++)
            {
                responses[i] = tokens == null ? replies[i].Speak : this.ResponseManager.Format(replies[i].Speak, tokens);
            }

            return responses;
        }
    }
}
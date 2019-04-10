// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace Microsoft.Bot.Builder.Solutions.Testing
{
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
            if (tokens == null)
            {
                return responses;
            }

            for (var i = 0; i < replies.Length; i++)
            {
                responses[i] = this.ResponseManager.Format(replies[i].Text, tokens);
            }

            return responses;
        }

        protected string[] ParseRepliesSpeak(string templateId, StringDictionary tokens = null)
        {
            var replies = ResponseManager.GetResponseTemplate(templateId).Replies;
            var responses = new string[replies.Length];
            if (tokens == null)
            {
                return responses;
            }

            for (var i = 0; i < replies.Length; i++)
            {
                responses[i] = this.ResponseManager.Format(replies[i].Speak, tokens);
            }

            return responses;
        }
    }
}
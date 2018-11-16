// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.AdaptiveCards;
using Microsoft.Bot.Solutions.Cards;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;

namespace Microsoft.Bot.Solutions.Dialogs
{
    /// <summary>
    /// Default Bot response builder.
    /// </summary>
    public class BotResponseBuilder : IBotResponseBuilder
    {
        private static readonly Regex SimpleTokensRegex = new Regex(@"\{(\w+)\}", RegexOptions.Compiled);
        private static readonly Regex ComplexTokensRegex = new Regex(@"\{[^{\}]+(?=})\}", RegexOptions.Compiled);

        private readonly IBotResponseFormatter defaultFormatter = new DefaultBotResponseFormatter();
        private readonly List<IBotResponseFormatter> responseFormatters = new List<IBotResponseFormatter>();

        /// <inheritdoc/>
        public void BuildAdaptiveCardReply<T>(Activity reply, BotResponse response, string cardPath, T cardDataAdapter, StringDictionary tokens = null)
            where T : CardDataBase
        {
            var tokensCopy = CopyTokens(tokens);
            var parsedResponse = this.ParseResponse(response, tokensCopy);
            var parsedCards = this.ParseAndCreateCards(cardPath, new List<T> { cardDataAdapter }, tokensCopy, parsedResponse);
            this.PopulateReplyFromResponse(reply, response);

            if (reply.Attachments == null)
            {
                reply.Attachments = new List<Attachment>();
            }

            reply.Attachments.Add(AdaptiveCardHelper.CreateCardAttachment(parsedCards[0]));

            if (response.SuggestedActions?.Length > 0)
            {
                reply.SuggestedActions = new SuggestedActions(
                    actions: response.SuggestedActions.Select(choice =>
                        new CardAction(
                            ActionTypes.ImBack,
                            choice,
                            value: choice.ToLower(),
                            displayText: choice.ToLower(),
                            text: choice.ToLower())).ToList());
            }
        }

        /// <inheritdoc/>
        public void BuildAdaptiveCardGroupReply<T>(Activity reply, BotResponse response, string cardPath, string attachmentLayout, List<T> cardDataAdapters, StringDictionary tokens = null)
            where T : CardDataBase
        {
            var tokensCopy = CopyTokens(tokens);
            var parsedResponse = this.ParseResponse(response, tokensCopy);
            var parsedCards = this.ParseAndCreateCards(cardPath, cardDataAdapters, tokensCopy, parsedResponse);
            this.PopulateReplyFromResponse(reply, response);

            if (cardDataAdapters.Count > 1)
            {
                reply.AttachmentLayout = attachmentLayout;
            }

            foreach (var card in parsedCards)
            {
                reply.Attachments.Add(AdaptiveCardHelper.CreateCardAttachment(card));
            }

            if (response.SuggestedActions?.Length > 0)
            {
                reply.SuggestedActions = new SuggestedActions(
                    actions: response.SuggestedActions.Select(choice =>
                        new CardAction(
                            ActionTypes.ImBack,
                            choice,
                            value: choice.ToLower(),
                            displayText: choice.ToLower(),
                            text: choice.ToLower())).ToList());
            }
        }

        /// <inheritdoc/>
        public void BuildMessageReply(Activity reply, BotResponse response, StringDictionary tokens = null)
        {
            var parsedResponse = this.ParseResponse(response, tokens);
            this.PopulateReplyFromResponse(reply, parsedResponse);
            if (parsedResponse.SuggestedActions?.Length > 0)
            {
                reply.SuggestedActions = new SuggestedActions(
                    actions: response.SuggestedActions.Select(choice =>
                        new CardAction(
                            ActionTypes.ImBack,
                            choice,
                            value: choice.ToLower(),
                            displayText: choice.ToLower(),
                            text: choice.ToLower())).ToList());
            }
        }

        /// <inheritdoc/>
        public void AddFormatter(IBotResponseFormatter formatter)
        {
            this.responseFormatters.Add(formatter);
        }

        /// <summary>
        /// Format the message using tokens.
        /// </summary>
        /// <param name="messageTemplate">default format function.</param>
        /// <param name="tokens">format values used to format the string.</param>
        /// <returns>formated string.</returns>
        public string Format(string messageTemplate, StringDictionary tokens)
        {
            var result = messageTemplate;
            var matches = ComplexTokensRegex.Matches(messageTemplate);
            foreach (var match in matches)
            {
                var formatted = false;
                var bindingJson = match.ToString();
                foreach (var formatter in this.responseFormatters)
                {
                    if (formatter.CanFormat(bindingJson))
                    {
                        result = formatter.FormatResponse(result, bindingJson, tokens);
                        formatted = true;
                        break;
                    }
                }

                if (!formatted)
                {
                    result = this.defaultFormatter.FormatResponse(result, bindingJson, tokens);
                }
            }

            return result;
        }

        /// <summary>
        /// Copy the tokens.
        /// </summary>
        /// <param name="tokens">Values need to add to bot response.</param>
        /// <returns>Copy of tokens.</returns>
        private static StringDictionary CopyTokens(StringDictionary tokens)
        {
            var tokensCopy = new StringDictionary();
            if (tokens != null)
            {
                foreach (string key in tokens.Keys)
                {
                    tokensCopy.Add(key, tokens[key]);
                }
            }

            return tokensCopy;
        }

        private List<AdaptiveCard> ParseAndCreateCards<T>(string cardPath, List<T> cardDataAdapters, StringDictionary tokens, BotResponse parsedResponse)
            where T : CardDataBase
        {
            if (!tokens.ContainsKey("Text"))
            {
                tokens.Add("Text", parsedResponse.Reply.Text);
            }

            if (!tokens.ContainsKey("Speak"))
            {
                tokens.Add("Speak", parsedResponse.Reply.Speak);
            }

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (!tokens.ContainsKey(property.Name))
                {
                    tokens.Add(property.Name, string.Empty);
                }
            }

            // Create list of cards.
            var cards = new List<AdaptiveCard>();
            foreach (var cardDataAdapter in cardDataAdapters)
            {
                // Add or update the public properties of the data type for the card to the tokens list.
                foreach (var property in properties)
                {
                    var value = cardDataAdapter.GetType().GetProperty(property.Name).GetValue(cardDataAdapter, null)?.ToString();
                    if (value != null)
                    {
                        value = SimpleTokensRegex.Replace(value, match => tokens[match.Groups[1].Value]);
                    }

                    if (!tokens.ContainsKey(property.Name))
                    {
                        tokens.Add(property.Name, value);
                    }
                    else
                    {
                        tokens[property.Name] = value;
                    }
                }

                cards.Add(AdaptiveCardHelper.GetCardFromJson(cardPath, tokens));
            }

            return cards;
        }

        private void PopulateReplyFromResponse(Activity reply, BotResponse response)
        {
            var replyTemplate = response.Reply;
            reply.Text = replyTemplate.Text;
            reply.Speak = replyTemplate.Speak;
            reply.InputHint = response.InputHint;
        }

        private BotResponse ParseResponse(BotResponse response, StringDictionary tokens)
        {
            foreach (var reply in response.Replies)
            {
                if (reply.Text != null)
                {
                    reply.Text = this.Format(reply.Text, tokens);
                }

                if (reply.Speak != null)
                {
                    reply.Speak = this.Format(reply.Speak, tokens);
                }
            }

            return response;
        }
    }
}
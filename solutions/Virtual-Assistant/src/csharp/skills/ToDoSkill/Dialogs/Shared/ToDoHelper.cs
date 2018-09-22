// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.Dialogs.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using global::ToDoSkill.Models;
    using Luis;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Solutions.Dialogs;
    using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;

    /// <summary>
    /// To Do skill helper class.
    /// </summary>
    public class ToDoHelper
    {
        private static readonly Regex ComplexTokensRegex = new Regex(@"\{[^{\}]+(?=})\}", RegexOptions.Compiled);
        private static readonly List<IBotResponseFormatter> ResponseFormatters = new List<IBotResponseFormatter>();
        private static readonly IBotResponseFormatter DefaultFormatter = new DefaultBotResponseFormatter();
        private static HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Generate httpClient.
        /// </summary>
        /// <param name="accessToken">API access token.</param>
        /// <returns>Generated httpClient.</returns>
        public static HttpClient GetHttpClient(string accessToken)
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return httpClient;
        }

        /// <summary>
        /// Generate ToAdaptiveCardAttachment.
        /// </summary>
        /// <param name="todos">To Do activities.</param>
        /// <param name="allTaskCount">all tasks count.</param>
        /// <param name="botResponse1">the bot response 1.</param>
        /// <param name="botResponse2">the bot response 2.</param>
        /// <returns>Generated adaptive card attachment.</returns>
        public static Microsoft.Bot.Schema.Attachment ToAdaptiveCardAttachmentForShowToDos(
            List<ToDoTaskActivityModel> todos,
            int allTaskCount,
            BotResponse botResponse1,
            BotResponse botResponse2)
        {
            var toDoCard = new AdaptiveCard();
            var speakText = Format(botResponse1.Reply.Speak, new StringDictionary() { { "taskCount", allTaskCount.ToString() } });
            if (botResponse2 != null)
            {
                speakText += Format(botResponse2.Reply.Speak, new StringDictionary() { { "taskCount", todos.Count.ToString() } });
            }

            var showText = Format(botResponse1.Reply.Text, new StringDictionary() { { "taskCount", allTaskCount.ToString() } });
            toDoCard.Speak = speakText;
            var body = new List<AdaptiveElement>();
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);
            var choiceSet = new AdaptiveChoiceSetInput();
            choiceSet.IsMultiSelect = true;
            string value = Guid.NewGuid().ToString() + ",";
            int index = 0;
            foreach (var todo in todos)
            {
                var choice = new AdaptiveChoice();
                choice.Title = todo.Topic;
                choice.Value = todo.Id;
                choiceSet.Choices.Add(choice);
                if (todo.IsCompleted)
                {
                    value += todo.Id + ",";
                }

                toDoCard.Speak += (++index) + "." + todo.Topic + " ";
            }

            value = value.Remove(value.Length - 1);
            choiceSet.Value = value;
            body.Add(choiceSet);
            toDoCard.Body = body;

            var attachment = new Microsoft.Bot.Schema.Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = toDoCard,
            };
            return attachment;
        }

        /// <summary>
        /// Generate ToAdaptiveCardAttachmentWithoutSpeech.
        /// </summary>
        /// <param name="todos">To Do activities.</param>
        /// <param name="allTaskCount">all tasks count.</param>
        /// <param name="taskContent">the task content.</param>
        /// <param name="botResponse1">the bot response 1.</param>
        /// <param name="botResponse2">the bot response 2.</param>
        /// <returns>Generated adaptive card attachment.</returns>
        public static Microsoft.Bot.Schema.Attachment ToAdaptiveCardAttachmentForOtherFlows(
            List<ToDoTaskActivityModel> todos,
            int allTaskCount,
            string taskContent,
            BotResponse botResponse1,
            BotResponse botResponse2)
        {
            var toDoCard = new AdaptiveCard();
            var showText = Format(botResponse2.Reply.Text, new StringDictionary() { { "taskCount", allTaskCount.ToString() } });
            var speakText = Format(botResponse1.Reply.Speak, new StringDictionary() { { "taskContent", taskContent } })
                + Format(botResponse2.Reply.Speak, new StringDictionary() { { "taskCount", allTaskCount.ToString() } });
            toDoCard.Speak = speakText;

            var body = new List<AdaptiveElement>();
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);
            var choiceSet = new AdaptiveChoiceSetInput();
            choiceSet.IsMultiSelect = true;
            string value = Guid.NewGuid().ToString() + ",";
            foreach (var todo in todos)
            {
                var choice = new AdaptiveChoice();
                choice.Title = todo.Topic;
                choice.Value = todo.Id;
                choiceSet.Choices.Add(choice);
                if (todo.IsCompleted)
                {
                    value += todo.Id + ",";
                }
            }

            value = value.Remove(value.Length - 1);
            choiceSet.Value = value;
            body.Add(choiceSet);
            toDoCard.Body = body;

            var attachment = new Microsoft.Bot.Schema.Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = toDoCard,
            };
            return attachment;
        }

        /// <summary>
        /// Generate response with token.
        /// </summary>
        /// <param name="botResponse">the bot response.</param>
        /// <param name="tokens">the tokens used by bot response.</param>
        /// <returns>Generated response.</returns>
        public static string GenerateResponseWithTokens(BotResponse botResponse, StringDictionary tokens)
        {
            return Format(botResponse.Reply.Text, tokens);
        }

        /// <summary>
        /// Digest luis result.
        /// </summary>
        /// <param name="context">dialog context.</param>
        /// <param name="accessors">the state accessors.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task DigestLuisResultAsync(ITurnContext context, ToDoSkillAccessors accessors)
        {
            try
            {
                var state = await accessors.ToDoSkillState.GetAsync(context);
                var luisResult = (ToDo)state.LuisResult;
                var entities = luisResult.Entities;
                if (luisResult.Entities.ContainsAll != null)
                {
                    state.MarkOrDeleteAllTasksFlag = true;
                }

                if (luisResult.Entities.ordinal != null)
                {
                    var index = (int)luisResult.Entities.ordinal[0];
                    if (index > 0 && index <= 5)
                    {
                        state.ToDoTaskIndex = index - 1;
                    }
                }

                if (context.Activity.Text != null)
                {
                    var words = context.Activity.Text.Split(' ');
                    foreach (var word in words)
                    {
                        if (word.Equals("all", StringComparison.OrdinalIgnoreCase))
                        {
                            state.MarkOrDeleteAllTasksFlag = true;
                        }
                    }
                }
            }
            catch
            {
                // ToDo
            }
        }

        /// <summary>
        /// Digest luis result.
        /// </summary>
        /// <param name="luisResult">Luis result.</param>
        /// <param name="userInput">User input.</param>
        /// <param name="toDoSkillState">To do skill state.</param>
        public static void DigestLuisResult(ToDo luisResult, string userInput, ref ToDoSkillState toDoSkillState)
        {
            var entities = luisResult.Entities;
            if (luisResult?.Entities?.ContainsAll != null)
            {
                toDoSkillState.MarkOrDeleteAllTasksFlag = true;
            }

            if (luisResult?.Entities?.ordinal != null)
            {
                var index = (int)luisResult.Entities.ordinal[0];
                if (index > 0 && index <= 5)
                {
                    toDoSkillState.ToDoTaskIndex = index - 1;
                }
            }

            if (!string.IsNullOrEmpty(userInput))
            {
                var words = userInput.Split(' ');
                foreach (var word in words)
                {
                    if (word.Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        toDoSkillState.MarkOrDeleteAllTasksFlag = true;
                    }
                }
            }
        }

        private static string Format(string messageTemplate, StringDictionary tokens)
        {
            var result = messageTemplate;
            var matches = ComplexTokensRegex.Matches(messageTemplate);
            foreach (var match in matches)
            {
                var formatted = false;
                var bindingJson = match.ToString();
                foreach (var formatter in ResponseFormatters)
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
                    result = DefaultFormatter.FormatResponse(result, bindingJson, tokens);
                }
            }

            return result;
        }
    }
}

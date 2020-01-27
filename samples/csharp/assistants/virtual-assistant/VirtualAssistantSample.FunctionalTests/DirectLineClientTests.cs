// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualAssistantSample.Tests;

namespace VirtualAssistantSample.FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    public class DirectLineClientTests : BotTestBase
    {
        private static string directLineSecret = "z-3WLu7PZKM.aVir2WPXwsefHv6ZYLzkHp0NNflU7oYf4ycVkP4D4as";
        private static string botId = "bf-virtual-assistant-nightly-lpahtc3";
        private static DirectLineClient client;
        private static LocaleTemplateEngineManager templateEngine;
        private static string fromUser = Guid.NewGuid().ToString();
        private static string fromUserName = "John Smith";

        // An event activity to trigger the welcome message (method for using custom Web Chat).
        private static Activity startConversationEvent = new Activity
        {
            From = new ChannelAccount(fromUser, fromUserName),
            Type = ActivityTypes.Event,
            Name = "startConversation",
            Locale = "en-us"
        };

        [TestInitialize]
        public void Test_Initialize()
        {
            GetEnvironmentVars();

            // Create a new Direct Line client.
            client = new DirectLineClient(directLineSecret);
        }

        [TestMethod]
        public async Task Test_Greeting()
        {
            await Assert_New_User_Greeting();
        }

        public async Task Assert_New_User_Greeting()
        {
            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, startConversationEvent);

            // Validate first Activity has attachment
            // Validate second Activity has text

            Assert.AreEqual(1, responses[0].Attachments.Count);
            // Assert.AreEqual(LocaleTemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("NamePrompt"));
        }

        //[TestMethod]
        //public async Task Test_Returning_User_Greeting()
        //{
        //    GetEnvironmentVars();

        //    var botAnswer = await StartBotConversationAsync();

        //    Assert.AreEqual($"Echo: {input}", botAnswer);
        //}

        /// <summary>
        /// Starts a conversation with a bot.
        /// </summary>
        /// <returns>Returns the new conversation.</returns>
        private static async Task<Conversation> StartBotConversationAsync()
        {
            // Start the conversation.
            return await client.Conversations.StartConversationAsync();
        }

        /// <summary>
        /// Sends an activity and waits for the response.
        /// </summary>
        /// <returns>Returns the bot's answer.</returns>
        private static async Task<List<Activity>> SendActivityAsync(Conversation conversation, Activity activity)
        {
            // Send the message activity to the bot.
            await client.Conversations.PostActivityAsync(conversation.ConversationId, activity);

            // Read the bot's message.
            var responses = await ReadBotMessagesAsync(client, conversation.ConversationId);

            return responses;
        }

        /// <summary>
        /// Polls the bot continuously until it gets a response.
        /// </summary>
        /// <param name="client">The Direct Line client.</param>
        /// <param name="conversationId">The conversation ID.</param>
        /// <returns>Returns the bot's answer.</returns>
        private static async Task<List<Activity>> ReadBotMessagesAsync(DirectLineClient client, string conversationId)
        {
            string watermark = null;
            List<Activity> botResponses = null;

            // Poll the bot for replies once per second.
            while (botResponses == null)
            {
                // Retrieve the activity sent from the bot.
                var activitySet = await client.Conversations.GetActivitiesAsync(conversationId, watermark);
                watermark = activitySet?.Watermark;

                // Extract the activities sent from the bot.
                var activities = from x in activitySet.Activities
                                 where x.From.Id == botId
                                 select x;

                // Analyze each activity in the activity set.
                if (activities.Any())
                {
                    botResponses = activities.ToList();
                }

                // Wait for one second before polling the bot again.
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                return botResponses;
            }

            return botResponses;
        }

        /// <summary>
        /// Get the values for the environment variables.
        /// </summary>
        private void GetEnvironmentVars()
        {
            if (string.IsNullOrWhiteSpace(directLineSecret) || string.IsNullOrWhiteSpace(botId))
            {
                directLineSecret = Environment.GetEnvironmentVariable("DIRECTLINE");
                if (string.IsNullOrWhiteSpace(directLineSecret))
                {
                    Assert.Inconclusive("Environment variable 'DIRECTLINE' not found.");
                }

                botId = Environment.GetEnvironmentVariable("BOTID");
                if (string.IsNullOrWhiteSpace(botId))
                {
                    Assert.Inconclusive("Environment variable 'BOTID' not found.");
                }
            }
        }
    }
}

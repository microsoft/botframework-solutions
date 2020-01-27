﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VirtualAssistantSample.FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    public class DirectLineClientTests
    {
        private static string directLineSecret = "z-3WLu7PZKM.aVir2WPXwsefHv6ZYLzkHp0NNflU7oYf4ycVkP4D4as";
        private static string botId = "bf-virtual-assistant-nightly-lpahtc3";
        private static string fromUser = "DirectLineClientTestUser";
        private static string echoGuid = string.Empty;
        private static string input = $"Testing Azure Bot GUID: ";

        [TestMethod]
        public async Task SendDirectLineMessage()
        {
            GetEnvironmentVars();

            echoGuid = Guid.NewGuid().ToString();
            input += echoGuid;

            var botAnswer = await StartBotConversationAsync();

            Assert.AreEqual($"Echo: {input}", botAnswer);
        }

        /// <summary>
        /// Starts a conversation with a bot. Sends a message and waits for the response.
        /// </summary>
        /// <returns>Returns the bot's answer.</returns>
        private static async Task<string> StartBotConversationAsync()
        {
            // Create a new Direct Line client.
            var client = new DirectLineClient(directLineSecret);

            // Start the conversation.
            var conversation = await client.Conversations.StartConversationAsync();

            // Create an event activity to trigger the welcome message.
            var startConversationEvent = new Activity
            {
                From = new ChannelAccount(fromUser),
                Type = ActivityTypes.Event,
                Name = "startConversation",
                Locale = "en-us"
            };

            // Send the message activity to the bot.
            await client.Conversations.PostActivityAsync(conversation.ConversationId, startConversationEvent);

            // Read the bot's message.
            var botAnswer = await ReadBotMessagesAsync(client, conversation.ConversationId);

            return botAnswer;
        }

        /// <summary>
        /// Polls the bot continuously until it gets a response.
        /// </summary>
        /// <param name="client">The Direct Line client.</param>
        /// <param name="conversationId">The conversation ID.</param>
        /// <returns>Returns the bot's answer.</returns>
        private static async Task<string> ReadBotMessagesAsync(DirectLineClient client, string conversationId)
        {
            string watermark = null;
            var answer = string.Empty;

            // Poll the bot for replies once per second.
            while (answer.Equals(string.Empty))
            {
                // Retrieve the activity sent from the bot.
                var activitySet = await client.Conversations.GetActivitiesAsync(conversationId, watermark);
                watermark = activitySet?.Watermark;

                // Extract the activities sent from the bot.
                var activities = from x in activitySet.Activities
                                 where x.From.Id == botId
                                 select x;

                // Analyze each activity in the activity set.
                foreach (var activity in activities)
                {
                    if (activity.Type == ActivityTypes.Message && activity.Text != "Welcome to Echo Bot.")
                    {
                        answer = activity.Text;
                    }
                }

                // Wait for one second before polling the bot again.
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);

                return answer;
            }

            return answer;
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

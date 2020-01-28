// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Tests;

namespace VirtualAssistantSample.FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    public class DirectLineClientTests : BotTestBase
    {
        private static string directLineSecret = string.Empty;
        private static string botId = string.Empty;
        private static DirectLineClient client;
        private static string fromUser = Guid.NewGuid().ToString();
        private static string testName = "Jane Doe";

        // An event activity to trigger the welcome message (method for using custom Web Chat).
        private static Activity startConversationEvent = new Activity
        {
            From = new ChannelAccount(fromUser, testName),
            Name = "startConversation",
            Type = ActivityTypes.Event
        };

        private static Activity testNameMessage = new Activity
        {
            From = new ChannelAccount(fromUser, testName),
            Text = testName,
            Type = ActivityTypes.Message
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

            await Assert_Returning_User_Greeting();
        }

        /// <summary>
        /// Assert that a new user is greeted with the onboarding prompt.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_New_User_Greeting()
        {
            var profileState = new UserProfileState();
            profileState.Name = testName;

            var allNamePromptVariations = LocaleTemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("NamePrompt");
            var allHaveMessageVariations = LocaleTemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("HaveNameMessage", profileState);

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, startConversationEvent);

            Assert.AreEqual(1, responses[0].Attachments.Count);
            CollectionAssert.Contains(allNamePromptVariations, responses[1].Text);

            responses = await SendActivityAsync(conversation, testNameMessage);

            CollectionAssert.Contains(allHaveMessageVariations, responses[2].Text);
        }

        /// <summary>
        /// Assert that a returning user is only greeted with a single card activity.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_Returning_User_Greeting()
        {
            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, startConversationEvent);

            Assert.AreEqual(1, responses.Count);
            Assert.AreEqual(1, responses[0].Attachments.Count);
        }

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

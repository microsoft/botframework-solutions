// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkillSample.Tests;

namespace SkillSample.FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    public class DirectLineClientTests : SkillTestBase
    {
        private static readonly string TestName = "Jane Doe";
        private static readonly string TriggerSkill = "Run Sample dialog";

        private static string _directLineSecret = "2tV9Y_8IYKI.ZRBhqyqJSFd1IQH4fJrHmA2OydbeHY7AboQ9NkKFoeg";
        private static string _botId = "SkillFunctionalTest-sg3wqo2";
        private static DirectLineClient _client;
        private static string _userID;

        [TestInitialize]
        public void Test_Initialize()
        {
            GetEnvironmentVars();

            // Create a new Direct Line client.
            _client = new DirectLineClient(_directLineSecret);
        }

        [TestMethod]
        public async Task Test_Greeting()
        {
            string fromUser = Guid.NewGuid().ToString();

            await Assert_New_User_Greeting(fromUser);
        }

        /// <summary>
        /// Assert that a new user is greeted with the intro text prompt.
        /// </summary>
        /// <param name="fromUser">User identifier used for the conversation and activities.</param>
        /// <returns>Task.</returns>
        public async Task Assert_New_User_Greeting(string fromUser)
        {
            var introText = GetTemplates("IntroText");
            var allFirstPromptTextVariations = GetTemplates("FirstPromptText");

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent(fromUser));

            Assert.AreEqual(introText[0], responses[0].Text);

            CollectionAssert.Contains(allFirstPromptTextVariations as ICollection, responses[1].Text);
        }

        /// <summary>
        /// Return a Start Conversation event with a customised UserId and Name enabling independent tests to not be affected by earlier functional test conversations.
        /// </summary>
        /// <param name="fromUser">User identifier used for the conversation and activities.</param>
        /// <returns>Event Activity with name startConversation.</returns>
        protected static Activity CreateStartConversationEvent(string fromUser)
        {
            // An event activity to trigger the welcome message (method for using custom Web Chat).
            return new Activity
            {
                From = new ChannelAccount(fromUser, TestName),
                Name = "startConversation",
                Type = ActivityTypes.Event
            };
        }

        /// <summary>
        /// Return a Message Activity with a customised UserId and Name enabling independent tests to not be affected by earlier functional test conversations.
        /// </summary>
        /// <param name="name">Name for Event Activity.</param>
        /// <param name="value">Value for Event Activity.</param>
        /// <returns>Event Activity with specified name.</returns>
        protected static Activity CreateEventActivity(string name, object value = null)
        {
            return new Activity
            {
                From = new ChannelAccount(_userID, TestName),
                Name = name,
                Value = value,
                Type = ActivityTypes.Event
            };
        }

        /// <summary>
        /// Return a Message Activity with a customised UserId and Name enabling independent tests to not be affected by earlier functional test conversations.
        /// </summary>
        /// <param name="fromUser">User identifier used for the conversation and activities.</param>
        private static Activity CreateMessageActivity(string fromUser, string activityText)
        {
            return new Activity
            {
                From = new ChannelAccount(fromUser, TestName),
                Text = activityText,
                Type = ActivityTypes.Message
            };
        }

        /// <summary>
        /// Starts a conversation with a bot.
        /// </summary>
        /// <returns>Returns the new conversation.</returns>
        private static async Task<Conversation> StartBotConversationAsync()
        {
            // Start the conversation.
            return await _client.Conversations.StartConversationAsync();
        }

        /// <summary>
        /// Sends an activity and waits for the response.
        /// </summary>
        /// <returns>Returns the bots answer.</returns>
        private static async Task<List<Activity>> SendActivityAsync(Conversation conversation, Activity activity)
        {
            // Send the message activity to the bot.
            await _client.Conversations.PostActivityAsync(conversation.ConversationId, activity);

            // Read the bot's message.
            var responses = await ReadBotMessagesAsync(_client, conversation.ConversationId);

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
                                where x.From.Id == _botId
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
            if (string.IsNullOrWhiteSpace(_directLineSecret) || string.IsNullOrWhiteSpace(_botId))
            {
                _directLineSecret = Environment.GetEnvironmentVariable("DIRECTLINE");
                if (string.IsNullOrWhiteSpace(_directLineSecret))
                {
                    Assert.Inconclusive("Environment variable 'DIRECTLINE' not found.");
                }

                _botId = Environment.GetEnvironmentVariable("BOTID");
                if (string.IsNullOrWhiteSpace(_botId))
                {
                    Assert.Inconclusive("Environment variable 'BOTID' not found.");
                }
            }
        }
    }
}

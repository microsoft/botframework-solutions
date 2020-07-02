using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualAssistantSample.Tests;

namespace VirtualAssistantSample.FunctionalTests
{
    public class DirectLineClientTestBase : BotTestBase
    {
        protected static readonly string TestName = "Jane Doe";

        private static string _directLineSecret = string.Empty;
        private static string _botId = string.Empty;
        private static DirectLineClient _client;
        private static string _userID;

        [TestInitialize]
        public void Test_Initialize()
        {
            GetEnvironmentVars();

            // Create a new Direct Line client.
            _client = new DirectLineClient(_directLineSecret);

            // Generate a user ID
            _userID = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Return a Start Conversation event with a customised UserId and Name enabling independent tests to not be affected by earlier functional test conversations.
        /// </summary>
        /// <param name="fromUser">User identifier used for the conversation and activities.</param>
        /// <returns>Event Activity with name startConversation.</returns>
        protected static Activity CreateStartConversationEvent()
        {
            // An event activity to trigger the welcome message (method for using custom Web Chat).
            return CreateEventActivity("startConversation");
        }

        /// <summary>
        /// Return a Message Activity with a customised UserId and Name enabling independent tests to not be affected by earlier functional test conversations.
        /// </summary>
        /// <param name="name">Name for Event Activity.</param>
        /// <returns>Event Activity with specified name.</returns>
        protected static Activity CreateEventActivity(string name)
        {
            return new Activity
            {
                From = new ChannelAccount(_userID, TestName),
                Name = name,
                Type = ActivityTypes.Event
            };
        }

        /// <summary>
        /// Return a Message Activity with a customised UserId and Name enabling independent tests to not be affected by earlier functional test conversations.
        /// </summary>
        /// <param name="text">Text for Message Activity.</param>
        /// <returns>Message Activity with specified text.</returns>
        protected static Activity CreateMessageActivity(string text)
        {
            return new Activity
            {
                From = new ChannelAccount(_userID, TestName),
                Text = text,
                Type = ActivityTypes.Message
            };
        }

        /// <summary>
        /// Starts a conversation with a bot.
        /// </summary>
        /// <returns>Direct Line Conversation object.</returns>
        protected static async Task<Conversation> StartBotConversationAsync()
        {
            // Start the conversation.
            return await _client.Conversations.StartConversationAsync();
        }

        /// <summary>
        /// Sends an activity and waits for the response.
        /// </summary>
        /// <returns>Returns the bots answer.</returns>
        protected static async Task<List<Activity>> SendActivityAsync(Conversation conversation, Activity activity)
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
        protected static async Task<List<Activity>> ReadBotMessagesAsync(DirectLineClient client, string conversationId)
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
                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

                return botResponses;
            }

            return botResponses;
        }

        /// <summary>
        /// Get the values for the environment variables.
        /// </summary>
        protected void GetEnvironmentVars()
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

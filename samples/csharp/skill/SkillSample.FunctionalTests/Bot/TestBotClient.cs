// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SkillSample.FunctionalTests.Configuration;

namespace SkillSample.FunctionalTests.Bot
{
    public class TestBotClient
    {
        private const string OriginHeaderKey = "Origin";
        private const string OriginHeaderValue = "https://skillsample.test.com";

        private readonly DirectLineClient directLineClient;
        private readonly IBotTestConfiguration config;
        private readonly string user = $"dl_FunctionalTestUser-{Guid.NewGuid()}";

        private string conversationId;
        private string token;
        private string watermark;

        public TestBotClient(IBotTestConfiguration config, string userId = null)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrEmpty(config.DirectLineSecret))
            {
                throw new ArgumentNullException(nameof(config.DirectLineSecret));
            }

            if (string.IsNullOrEmpty(config.BotId))
            {
                throw new ArgumentNullException(nameof(config.BotId));
            }

            if (!string.IsNullOrEmpty(userId))
            {
                user = userId;
            }

            // Instead of generating a vanilla DirectLineClient with secret,
            // we obtain a directline token with the secrets and then we use
            // that token to create the directline client.
            // What this gives us is the ability to pass TrustedOrigins when obtaining the token,
            // which tests the enhanced authentication.
            // This endpoint is unfortunately not supported by the directline client which is
            // why we add this custom code.
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"https://directline.botframework.com/v3/directline/tokens/generate");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.DirectLineSecret);
                request.Content = new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        User = new { Id = this.user },
                        TrustedOrigins = new string[]
                        {
                            OriginHeaderValue
                        }
                    }), Encoding.UTF8, "application/json");

                using (var response = client.SendAsync(request).GetAwaiter().GetResult())
                {
                    if (response.IsSuccessStatusCode)
                    {
                        // Extract token from response
                        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        this.token = JsonConvert.DeserializeObject<DirectLineToken>(body).Token;
                        this.conversationId = JsonConvert.DeserializeObject<DirectLineToken>(body).ConversationId;

                        // Create directline client from token
                        this.directLineClient = new DirectLineClient(token);

                        // From now on, we'll add an Origin header in directline calls, with
                        // the trusted origin we sent when acquiring the token as value.
                        directLineClient.HttpClient.DefaultRequestHeaders.Add(OriginHeaderKey, OriginHeaderValue);
                    }
                    else
                    {
                        throw new Exception("Failed to acquire directline token");
                    }
                }
            }
        }

        public string GetUser()
        {
            return user;
        }

        public Task<ResourceResponse> SendMessageAsync(string message, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(nameof(message)))
            {
                throw new ArgumentNullException(nameof(message));
            }

            // Create a message activity with the input text.
            var messageActivity = new Activity
            {
                From = new ChannelAccount(this.user),
                Text = message,
                Type = ActivityTypes.Message,
            };

            Console.WriteLine($"Sent to bot: {message}");
            return SendActivityAsync(messageActivity, cancellationToken);
        }

        public Task<ResourceResponse> SendEventAsync(string name, object value = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(nameof(name)))
            {
                throw new ArgumentNullException(nameof(name));
            }

            // Create a message activity with the input text.
            var eventActivity = new Activity
            {
                From = new ChannelAccount(this.user),
                Name = name,
                Value = value,
                Type = ActivityTypes.Event,
            };

            Console.WriteLine($"Sent event to bot: {name}");
            return SendActivityAsync(eventActivity, cancellationToken);
        }

        public async Task<ResourceResponse[]> SendMessagesAsync(IEnumerable<string> messages, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            var resourceResponses = new List<ResourceResponse>();

            foreach (var message in messages)
            {
                resourceResponses.Add(await SendMessageAsync(message, cancellationToken));
            }

            return resourceResponses.ToArray();
        }

        public async Task StartConversation(CancellationToken cancellationToken = default(CancellationToken))
        {
            var conversation = await directLineClient.Conversations.StartConversationAsync(cancellationToken);
            this.conversationId = conversation?.ConversationId ?? throw new InvalidOperationException("Conversation cannot be null");
        }

        public Task<ResourceResponse> SendActivityAsync(Activity activity, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Send the message activity to the bot.
            return directLineClient.Conversations.PostActivityAsync(this.conversationId, activity, cancellationToken);
        }

        public async Task AssertReplyAsync(string expected, CancellationToken cancellationToken = default(CancellationToken))
        {
            var messages = await PollBotMessagesAsync(cancellationToken);
            Console.WriteLine("Messages sent from bot:");
            var messagesList = messages.ToList();
            foreach (var m in messagesList.ToList())
            {
                Console.WriteLine($"Type:{m.Type}; Text:{m.Text}");
            }

            Assert.IsTrue(messagesList.Any(m => m.Type == ActivityTypes.Message && m.Text.Contains(expected, StringComparison.OrdinalIgnoreCase)), $"Expected: {expected}");
        }

        public async Task AssertReplyOneOf(IEnumerable<object> expected, CancellationToken cancellationToken = default(CancellationToken))
        {
            var messages = await PollBotMessagesAsync(cancellationToken);
            Assert.IsTrue(messages.Any(m => m.Type == ActivityTypes.Message && expected.Any(e => m.Text.Contains(e.ToString(), StringComparison.OrdinalIgnoreCase))));
        }

        public async Task<IEnumerable<Activity>> PollBotMessagesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Even if we receive a cancellation token with a super long timeout,
            // we set a cap on the max time this while loop can run
            var maxCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            while (!cancellationToken.IsCancellationRequested && !maxCancellation.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));

                var activities = await ReadBotMessagesAsync(cancellationToken);

                if (activities != null && activities.Any())
                {
                    return activities.Where(activity => activity.Type.Equals(ActivityTypes.Message));
                }
            }

            throw new Exception("No activities received");
        }

        public async Task<IEnumerable<Activity>> ReadBotMessagesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Retrieve activities from directline
            var activitySet = await directLineClient.Conversations.GetActivitiesAsync(conversationId, watermark, cancellationToken);
            watermark = activitySet?.Watermark;

            // Extract and return the activities sent from the bot.
            return activitySet == null ? null : activitySet?.Activities?.Where(activity => activity.From.Id == this.config.BotId && activity.Type == ActivityTypes.Message);
        }
    }

    public class DirectLineToken
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("conversationId")]
        public string ConversationId { get; set; }
    }

    public class DirectLineSession
    {
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
    }

    public class DirectLineSessionInfo
    {
        public string SessionId { get; set; }

        public Cookie Cookie { get; set; }
    }
}

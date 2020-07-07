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
using AdaptiveCards;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using VirtualAssistantSample.FunctionalTests.Configuration;

namespace VirtualAssistantSample.FunctionalTests
{
    public class TestBotClient
    {
        private const string OriginHeaderKey = "Origin";
        private const string OriginHeaderValue = "https://bfsolutions.test.com";

        private readonly DirectLineClient directLineClient;
        private readonly IBotTestConfiguration config;
        private readonly string user = $"dl_SkillTestUser-{Guid.NewGuid()}";

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

        public Task<ResourceResponse> SendEventAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
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
                await Task.Delay(TimeSpan.FromSeconds(3));

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

        public async Task VerifyAdaptiveCard(IEnumerable<object> expected, Activity adaptiveCard, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (adaptiveCard == null)
            {
                throw new Exception("AdaptiveCard is null");
            }
            else if (adaptiveCard.Attachments == null)
            {
                throw new Exception("AdaptiveCard.Attachments = null");
            }

            var card = JsonConvert.DeserializeObject<AdaptiveCard>(JsonConvert.SerializeObject(adaptiveCard.Attachments.FirstOrDefault().Content));

            if (card == null)
            {
                throw new Exception("No Adaptive Card received in activity");
            }

            if (card.Speak == null)
            {
                throw new Exception("No speak received in Adaptive Card");
            }

            // Verify that the speak property matches one of expected replies
            Assert.IsTrue(expected.Any(e => card.Speak.Contains(e.ToString(), StringComparison.OrdinalIgnoreCase)));
        }

        public async Task VerifyHeroCard(IEnumerable<object> expected, Activity heroCard, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (heroCard == null)
            {
                throw new Exception("HeroCard is null");
            }
            else if (heroCard.Attachments == null)
            {
                throw new Exception("HeroCard.Attachments = null");
            }

            var card = JsonConvert.DeserializeObject<HeroCard>(JsonConvert.SerializeObject(heroCard.Attachments.FirstOrDefault().Content));

            if (card == null)
            {
                throw new Exception("No Hero Card received in activity");
            }

            if (card.Subtitle == null)
            {
                throw new Exception("No subtitle received in hero Card");
            }

            // Verify that the speak property matches one of expected replies
            Assert.IsTrue(expected.Any(e => card.Subtitle.Contains(e.ToString(), StringComparison.OrdinalIgnoreCase)));
        }

        public async Task SignInAndVerifyOAuthAsync(Activity oAuthCard, CancellationToken cancellationToken = default(CancellationToken))
        {
            // We obtained what we think is an OAuthCard. Steps to follow:
            // 1- Verify we have a sign in link
            // 2- Get directline session id and cookie
            // 3- Follow sign in link but manually do each redirect
            //      3.a- Detect the PostSignIn url in the redirect chain
            //      3.b- Add cookie and challenge session id to post sign in link
            // 4- Verify final redirect to token service ends up in success

            // 1- Verify we have a sign in link in the activity
            if (oAuthCard == null)
            {
                throw new Exception("OAuthCard is null");
            }
            else if (oAuthCard.Attachments == null)
            {
                throw new Exception("OAuthCard.Attachments = null");
            }

            var card = JsonConvert.DeserializeObject<SigninCard>(JsonConvert.SerializeObject(oAuthCard.Attachments.FirstOrDefault().Content));

            if (card == null)
            {
                throw new Exception("No SignIn Card received in activity");
            }

            if (card.Buttons == null || !card.Buttons.Any())
            {
                throw new Exception("No buttons received in sign in card");
            }

            string signInUrl = card.Buttons[0].Value?.ToString();

            if (string.IsNullOrEmpty(signInUrl) || !signInUrl.StartsWith("https://"))
            {
                throw new Exception($"Sign in url is empty or badly formatted. Url received: {signInUrl}");
            }

            // 2- Get directline session id and cookie
            var sessionInfo = await GetSessionInfoAsync();

            // 3- Follow sign in link but manually do each redirect
            // 4- Verify final redirect to token service ends up in success
            await SignInAsync(sessionInfo, signInUrl);
        }

        private async Task SignInAsync(DirectLineSessionInfo directLineSession, string url)
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false,
                CookieContainer = cookieContainer
            };

            // We have a sign in url, which will produce multiple HTTP 302 for redirects
            // This will path
            //      token service -> other services -> auth provider -> token service (post sign in)-> response with token
            // When we receive the post sign in redirect, we add the cookie passed in the directline session info
            // to test enhanced authentication. This in ther scenarios happens by itself since browsers do this for us.
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add(OriginHeaderKey, OriginHeaderValue);

                while (!string.IsNullOrEmpty(url))
                {
                    using (var response = await client.GetAsync(url))
                    {
                        var text = await response.Content.ReadAsStringAsync();

                        url = response.StatusCode == HttpStatusCode.Redirect
                            ? response.Headers.Location.OriginalString
                            : null;

                        // Once the redirects are done, there is no more url. This means we
                        // did the entire loop
                        if (url == null)
                        {
                            Assert.IsTrue(response.IsSuccessStatusCode);
                            Assert.IsTrue(text.Contains("You are now signed in and can close this window."));
                            return;
                        }

                        // If this is the post sign in callback, add the cookie and code challenge
                        // so that the token service gets the verification.
                        // Here we are simulating what Webchat does along with the browser cookies.
                        if (url.StartsWith("https://token.botframework.com/api/oauth/PostSignInCallback"))
                        {
                            url += $"&code_challenge={directLineSession.SessionId}";
                            cookieContainer.Add(directLineSession.Cookie);
                        }
                    }
                }

                throw new Exception("Sign in did not succeed. Set a breakpoint in TestBotClient.SignInAsync() to debug the redirect sequence.");
            }
        }

        private async Task<DirectLineSessionInfo> GetSessionInfoAsync()
        {
            // Set up cookie container to obtain response cookie
            CookieContainer cookies = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookies;

            using (var client = new HttpClient(handler))
            {
                // Call the directline session api, not supported by DirectLine client
                const string getSessionUrl = "https://directline.botframework.com/v3/directline/session/getsessionid";
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, getSessionUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.token);

                // We want to add the Origins header to this client as well
                client.DefaultRequestHeaders.Add(OriginHeaderKey, OriginHeaderValue);


                using (var response = await client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        // The directline response that is relevant to us is the cookie and the session info.

                        // Extract cookie from cookies
                        var cookie = cookies.GetCookies(new Uri(getSessionUrl)).Cast<Cookie>().FirstOrDefault(c => c.Name == "webchat_session_v2");

                        // Extract session info from body
                        var body = await response.Content.ReadAsStringAsync();
                        var session = JsonConvert.DeserializeObject<DirectLineSession>(body);

                        return new DirectLineSessionInfo()
                        {
                            SessionId = session.SessionId,
                            Cookie = cookie
                        };
                    }
                    else
                    {
                        throw new Exception("Failed to obtain session id");
                    }
                }
            }
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

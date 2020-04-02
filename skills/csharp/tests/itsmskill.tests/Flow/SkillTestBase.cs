// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using ITSMSkill.Bots;
using ITSMSkill.Dialogs;
using ITSMSkill.Responses.Knowledge;
using ITSMSkill.Responses.Main;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Responses.Ticket;
using ITSMSkill.Services;
using ITSMSkill.Tests.API.Fakes;
using ITSMSkill.Tests.Flow.Fakes;
using ITSMSkill.Tests.Flow.Utterances;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Solutions.Proactive;
using ITSMSkill.Models;

namespace ITSMSkill.Tests.Flow
{
    public class SkillTestBase : BotTestBase
    {
        public static readonly string AuthenticationName = "ServiceNow";

        public static readonly string AuthenticationProvider = "Generic Oauth 2";

        public static readonly string MagicCode = "000000";

        public static readonly string TestToken = "TestToken";

        public IConversationUpdateActivity StartActivity { get; set; }

        public IServiceCollection Services { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            // Initialize service collection
            Services = new ServiceCollection();

            // Load settings
            var settings = new BotSettings();
            settings.OAuthConnections = new List<OAuthConnection>()
            {
                new OAuthConnection() { Name = AuthenticationProvider, Provider = AuthenticationProvider }
            };
            settings.LimitSize = MockData.LimitSize;
            settings.ServiceNowUrl = MockData.ServiceNowUrl;
            settings.ServiceNowGetUserId = MockData.ServiceNowGetUserId;
            settings.MicrosoftAppId = MockData.MicrosoftAppId;
            settings.MicrosoftAppPassword = MockData.MicrosoftAppPassword;
            Services.AddSingleton(settings);
            Services.AddSingleton<BotSettingsBase>(settings);

            // Configure credentials

            // Configure telemetry
            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();

            // Configure bot services
            Services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {
                        "en-us", new CognitiveModelSet()
                        {
                            LuisServices = new Dictionary<string, LuisRecognizer>
                            {
                                {
                                    "General", new BaseMockLuisRecognizer<GeneralLuis>(
                                        new GeneralTestUtterances())
                                },
                                {
                                    "ITSM", new BaseMockLuisRecognizer<ITSMLuis>(
                                        new TicketCloseUtterances(),
                                        new TicketCreateUtterances(),
                                        new TicketShowUtterances(),
                                        new TicketUpdateUtterances(),
                                        new KnowledgeShowUtterances())
                                }
                            }
                        }
                    }
                }
            });

            // Configure storage
            Services.AddSingleton<IStorage, MemoryStorage>();
            Services.AddSingleton<UserState>();
            Services.AddSingleton<ConversationState>();
            Services.AddSingleton<ProactiveState>();
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                return new BotStateSet(userState, conversationState);
            });

            // Configure proactive
            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddHostedService<QueuedHostedService>();

            // Configure responses
            ResponseManager = new ResponseManager(
                new string[] { "en", "de", "es", "fr", "it", "zh" },
                new MainResponses(),
                new TicketResponses(),
                new KnowledgeResponses(),
                new SharedResponses());
            Services.AddSingleton(ResponseManager);

            // Configure service
            Services.AddSingleton<IServiceManager, MockServiceManager>();

            // Register dialogs
            Services.AddTransient<CreateTicketDialog>();
            Services.AddTransient<UpdateTicketDialog>();
            Services.AddTransient<ShowTicketDialog>();
            Services.AddTransient<CloseTicketDialog>();
            Services.AddTransient<ShowKnowledgeDialog>();
            Services.AddTransient<MainDialog>();
            Services.AddSingleton(new MicrosoftAppCredentials(settings.MicrosoftAppId, settings.MicrosoftAppPassword));

            // Configure adapters
            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();

            // Configure bot
            Services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();
        }

        protected TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();


            StartActivity = Activity.CreateConversationUpdateActivity();
            StartActivity.MembersAdded = new ChannelAccount[] { adapter.Conversation.User, adapter.Conversation.Bot };

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        protected TestFlow GetSkillTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            adapter.AddUserToken(AuthenticationProvider, adapter.Conversation.ChannelId, adapter.Conversation.User.Id, TestToken, MagicCode);

            StartActivity = Activity.CreateConversationUpdateActivity();
            StartActivity.MembersAdded = new ChannelAccount[] { adapter.Conversation.User, adapter.Conversation.Bot };

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                // Set claims in turn state to simulate skill mode
                var claims = new List<Claim>();
                claims.Add(new Claim(AuthenticationConstants.VersionClaim, "1.0"));
                claims.Add(new Claim(AuthenticationConstants.AudienceClaim, Guid.NewGuid().ToString()));
                claims.Add(new Claim(AuthenticationConstants.AppIdClaim, Guid.NewGuid().ToString()));
                context.TurnState.Add("BotIdentity", new ClaimsIdentity(claims));

                var bot = sp.GetService<IBot>();
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        protected Action<IActivity> ActionEndMessage()
        {
            return AssertContains(MainResponses.WelcomeMessage);
        }

        protected Action<IActivity> SkillActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }

        protected Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var message = activity.AsMessageActivity();
                Assert.AreEqual(1, message.Attachments.Count);
                Assert.AreEqual("application/vnd.microsoft.card.oauth", message.Attachments[0].ContentType);
            };
        }

        protected Action<IActivity> AssertStartsWith(string response, StringDictionary tokens = null, params string[] cardIds)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                if (response == null)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(messageActivity.Text));
                }
                else
                {
                    var collection = ParseReplies(response, tokens ?? new StringDictionary());
                    Assert.IsTrue(collection.Any((reply) =>
                    {
                        return messageActivity.Text.StartsWith(reply);
                    }));
                }

                AssertSameId(messageActivity, cardIds);
            };
        }

        protected Action<IActivity> AssertContains(string response, StringDictionary tokens = null, params string[] cardIds)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                if (response == null)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(messageActivity.Text));
                }
                else
                {
                    var collection = ParseReplies(response, tokens ?? new StringDictionary());
                    CollectionAssert.Contains(collection, messageActivity.Text);
                }

                AssertSameId(messageActivity, cardIds);
            };
        }

        private void AssertSameId(IMessageActivity activity, string[] cardIds = null)
        {
            if (cardIds == null)
            {
                Assert.AreEqual(activity.Attachments.Count, 0);
                return;
            }

            Assert.AreEqual(activity.Attachments.Count, cardIds.Length);

            for (int i = 0; i < cardIds.Length; ++i)
            {
                var card = activity.Attachments[i].Content as JObject;
                Assert.AreEqual(card["id"], cardIds[i]);
            }
        }
    }
}

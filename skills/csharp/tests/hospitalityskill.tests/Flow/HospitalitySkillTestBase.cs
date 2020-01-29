// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using HospitalitySkill.Bots;
using HospitalitySkill.Dialogs;
using HospitalitySkill.Responses.CheckOut;
using HospitalitySkill.Responses.ExtendStay;
using HospitalitySkill.Responses.GetReservation;
using HospitalitySkill.Responses.LateCheckOut;
using HospitalitySkill.Responses.Main;
using HospitalitySkill.Responses.RequestItem;
using HospitalitySkill.Responses.RoomService;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Services;
using HospitalitySkill.Tests.Flow.Fakes;
using HospitalitySkill.Tests.Flow.Utterances;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace HospitalitySkill.Tests.Flow
{
    public class HospitalitySkillTestBase : BotTestBase
    {
        public static readonly DateTime CheckInDate = DateTime.Now;

        public IServiceCollection Services { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            // Initialize service collection
            Services = new ServiceCollection();

            // Load settings
            var settings = new BotSettings();
            Services.AddSingleton(settings);
            Services.AddSingleton<BotSettingsBase>(settings);

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
                                    "Hospitality", new BaseMockLuisRecognizer<HospitalityLuis>(
                                        new CheckOutUtterances(),
                                        new ExtendStayUtterances(),
                                        new GetReservationUtterances(),
                                        new LateCheckOutUtterances(),
                                        new RequestItemUtterances(),
                                        new RoomServiceUtterances())
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
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                return new BotStateSet(userState, conversationState);
            });

            // Configure proactive
            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddHostedService<QueuedHostedService>();

            // Configure services
            Services.AddSingleton<IHotelService>(new HotelService(CheckInDate));

            // Configure responses
            ResponseManager = new ResponseManager(
                new string[] { "en", "de", "es", "fr", "it", "zh" },
                new MainResponses(),
                new SharedResponses(),
                new CheckOutResponses(),
                new LateCheckOutResponses(),
                new ExtendStayResponses(),
                new GetReservationResponses(),
                new RequestItemResponses(),
                new RoomServiceResponses());
            Services.AddSingleton(ResponseManager);

            // Register dialogs
            Services.AddTransient<CheckOutDialog>();
            Services.AddTransient<LateCheckOutDialog>();
            Services.AddTransient<ExtendStayDialog>();
            Services.AddTransient<GetReservationDialog>();
            Services.AddTransient<RequestItemDialog>();
            Services.AddTransient<RoomServiceDialog>();
            Services.AddTransient<MainDialog>();

            // Configure adapters
            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();

            // Configure bot
            Services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();
        }

        protected TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        protected Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
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
                if (cardIds[i] == HeroCard.ContentType)
                {
                    Assert.IsTrue(activity.Attachments[i].Content is HeroCard);
                }
                else
                {
                    var card = activity.Attachments[i].Content as JObject;
                    Assert.AreEqual(card["id"], cardIds[i]);
                }
            }
        }
    }
}

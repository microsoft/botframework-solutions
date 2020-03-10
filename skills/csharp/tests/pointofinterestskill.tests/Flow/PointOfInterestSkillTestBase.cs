// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Models;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PointOfInterestSkill.Bots;
using PointOfInterestSkill.Dialogs;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Responses.CancelRoute;
using PointOfInterestSkill.Responses.FindPointOfInterest;
using PointOfInterestSkill.Responses.Main;
using PointOfInterestSkill.Responses.Route;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Tests.API.Fakes;
using PointOfInterestSkill.Tests.Flow.Utterances;
using SkillServiceLibrary.Fakes.AzureMapsAPI.Fakes;

namespace PointOfInterestSkill.Tests.Flow
{
    public class PointOfInterestSkillTestBase : BotTestBase
    {
        public IServiceCollection Services { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            // Initialize service collection
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings()
            {
                AzureMapsKey = MockData.Key
            });
            Services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {
                        "en", new CognitiveModelSet()
                        {
                            LuisServices = new Dictionary<string, LuisRecognizer>
                            {
                                { "General", new Fakes.MockGeneralLuisRecognizer() },
                                {
                                    "PointOfInterest", new Fakes.MockPointOfInterestLuisRecognizer(
                                    new FindParkingUtterances(),
                                    new FindPointOfInterestUtterances(),
                                    new RouteFromXToYUtterances())
                                }
                            }
                        }
                    }
                }
            });

            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            Services.AddSingleton(new UserState(new MemoryStorage()));
            Services.AddSingleton(new ConversationState(new MemoryStorage()));
            Services.AddSingleton(new ProactiveState(new MemoryStorage()));
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                var proactiveState = sp.GetService<ProactiveState>();
                return new BotStateSet(userState, conversationState);
            });

            ResponseManager = new ResponseManager(
                new string[] { "en", "de", "es", "fr", "it", "zh" },
                new POISharedResponses(),
                new RouteResponses(),
                new FindPointOfInterestResponses(),
                new POIMainResponses(),
                new CancelRouteResponses());

            Services.AddSingleton(ResponseManager);

            Services.AddSingleton<IServiceManager, MockServiceManager>();
            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();
            Services.AddTransient<MainDialog>();
            Services.AddTransient<CancelRouteDialog>();
            Services.AddTransient<FindParkingDialog>();
            Services.AddTransient<FindPointOfInterestDialog>();
            Services.AddTransient<RouteDialog>();
            Services.AddTransient<GetDirectionsDialog>();
            Services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();

            var mockHttpContext = new DefaultHttpContext();
            mockHttpContext.Request.Scheme = "http";
            mockHttpContext.Request.Host = new HostString("localhost", 3980);
            Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = mockHttpContext });
        }

        public TestFlow GetTestFlow()
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

        public TestFlow GetSkillTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

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

        protected Action<IActivity> AssertStartsWith(string response, IList<string> cardIds)
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
                    Assert.IsTrue(ParseReplies(response, new StringDictionary()).Any((reply) =>
                    {
                        return messageActivity.Text.StartsWith(reply);
                    }));
                }

                AssertSameId(messageActivity, cardIds);
            };
        }

        protected Action<IActivity> AssertContains(string response, IList<string> cardIds)
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
                    CollectionAssert.Contains(ParseReplies(response, new StringDictionary()), messageActivity.Text);
                }

                AssertSameId(messageActivity, cardIds);
            };
        }

        protected void AssertSameId(IMessageActivity activity, IList<string> cardIds = null)
        {
            if (cardIds == null)
            {
                return;
            }

            for (int i = 0; i < cardIds.Count; ++i)
            {
                var card = activity.Attachments[i].Content as JObject;
                Assert.AreEqual(card["id"], cardIds[i]);
            }
        }

        /// <summary>
        /// Asserts bot response of Event Activity.
        /// </summary>
        /// <returns>Returns an Action with IActivity object.</returns>
        protected Action<IActivity> CheckForEvent(PointOfInterestDialogBase.OpenDefaultAppType openDefaultAppType = PointOfInterestDialogBase.OpenDefaultAppType.Map)
        {
            return activity =>
            {
                var eventReceived = activity.AsEventActivity()?.Value as OpenDefaultApp;
                Assert.IsNotNull(eventReceived, "Activity received is not an Event as expected");
                if (openDefaultAppType == PointOfInterestDialogBase.OpenDefaultAppType.Map)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(eventReceived.MapsUri));
                }
                else if (openDefaultAppType == PointOfInterestDialogBase.OpenDefaultAppType.Telephone)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(eventReceived.TelephoneUri));
                }
            };
        }

        protected Action<IActivity> CheckForEoC(bool value = false)
        {
            return activity =>
            {
                var eoc = (Activity)activity;
                Assert.AreEqual(ActivityTypes.EndOfConversation, eoc.Type);
                if (value)
                {
                    var dest = eoc.Value as SingleDestinationResponse;
                    Assert.IsNotNull(dest);
                    Assert.IsTrue(!string.IsNullOrEmpty(dest.Name));
                }
            };
        }
    }
}
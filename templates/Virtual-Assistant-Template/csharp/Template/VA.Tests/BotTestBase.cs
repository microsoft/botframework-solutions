﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Feedback;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using $ext_safeprojectname$.Bots;
using $ext_safeprojectname$.Dialogs;
using $ext_safeprojectname$.Services;
using $safeprojectname$.Utilities;

namespace $safeprojectname$
{
    public class BotTestBase
    {
        public IServiceCollection Services { get; set; }

        [TestInitialize]
        public virtual void Initialize()
        {
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings());
            Services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {
                        "en-us", new CognitiveModelSet
                        {
                            DispatchService = DispatchTestUtil.CreateRecognizer(),
                            LuisServices = new Dictionary<string, ITelemetryRecognizer>
                            {
                                { "General", GeneralTestUtil.CreateRecognizer() }
                            },
                            QnAServices = new Dictionary<string, ITelemetryQnAMaker>
                            {
                                { "Faq", FaqTestUtil.CreateRecognizer() },
                                { "Chitchat", ChitchatTestUtil.CreateRecognizer() }
                            }
                        }
                    }
                }
            });

            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            Services.AddSingleton(new MicrosoftAppCredentials("appId", "password"));
            Services.AddSingleton(new UserState(new MemoryStorage()));
            Services.AddSingleton(new ConversationState(new MemoryStorage()));
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                return new BotStateSet(userState, conversationState);
            });

            Services.AddTransient<CancelDialog>();
            Services.AddTransient<EscalateDialog>();
            Services.AddTransient<MainDialog>();
            Services.AddTransient<OnboardingDialog>();
            Services.AddTransient<List<SkillDialog>>();
            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();
            Services.AddTransient<IBot, DialogBot<MainDialog>>();
        }

        public TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>()
                .Use(new FeedbackMiddleware(sp.GetService<ConversationState>(), sp.GetService<IBotTelemetryClient>()));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }
    }
}
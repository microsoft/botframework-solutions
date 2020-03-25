// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Feedback;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills.Dialogs;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualAssistantSample.Bots;
using VirtualAssistantSample.Dialogs;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Services;
using VirtualAssistantSample.Tests.Utilities;

namespace VirtualAssistantSample.Tests
{
    public class BotTestBase
    {
        public IServiceCollection Services { get; set; }

        public LocaleTemplateManager TestLocaleTemplateManager { get; set; }

        public UserProfileState TestUserProfileState { get; set; }

        protected Templates AllResponsesTemplates
        {
            get
            {
                var path = CultureInfo.CurrentUICulture.Name.ToLower() == "en-us" ?
                    Path.Combine(".", "Responses", $"AllResponses.lg") :
                    Path.Combine(".", "Responses", $"AllResponses.{CultureInfo.CurrentUICulture.Name.ToLower()}.lg");
                return Templates.ParseFile(path);
            }
        }

        [TestInitialize]
        public virtual void Initialize()
        {
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings());
            Services.AddSingleton(new BotServices()
            {
                // Non US languages are empty as Dispatch/LUIS not required for localization tests.
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {
                        "en-us", new CognitiveModelSet
                        {
                            DispatchService = DispatchTestUtil.CreateRecognizer(),
                            LuisServices = new Dictionary<string, LuisRecognizer>
                            {
                                { "General", GeneralTestUtil.CreateRecognizer() }
                            },
                        }
                    },
                    {
                        "zh-cn", new CognitiveModelSet { }
                    },
                    {
                        "fr-fr", new CognitiveModelSet { }
                    },
                    {
                        "es-es", new CognitiveModelSet { }
                    },
                    {
                        "de-de", new CognitiveModelSet { }
                    },
                    {
                        "it-it", new CognitiveModelSet { }
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

            // For localization testing
            CultureInfo.CurrentUICulture = new CultureInfo("en-us");

            var localizedTemplates = new Dictionary<string, string>();
            var templateFile = "AllResponses";
            var supportedLocales = new List<string>() { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" };

            foreach (var locale in supportedLocales)
            {
                // LG template for en-us does not include locale in file extension.
                var localeTemplateFile = locale.Equals("en-us")
                    ? Path.Combine(".", "Responses", $"{templateFile}.lg")
                    : Path.Combine(".", "Responses", $"{templateFile}.{locale}.lg");

                localizedTemplates.Add(locale, localeTemplateFile);
            }

            TestLocaleTemplateManager = new LocaleTemplateManager(localizedTemplates, "en-us");
            Services.AddSingleton(TestLocaleTemplateManager);

            Services.AddTransient<MainDialog>();
            Services.AddTransient<OnboardingDialog>();
            Services.AddTransient<SwitchSkillDialog>();
            Services.AddTransient<List<SkillDialog>>();
            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();
            Services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();

            TestUserProfileState = new UserProfileState();
            TestUserProfileState.Name = "Bot";
        }

        public TestFlow GetTestFlow(bool includeUserProfile = true)
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>()
                .Use(new FeedbackMiddleware(sp.GetService<ConversationState>(), sp.GetService<IBotTelemetryClient>()));
            var userState = sp.GetService<UserState>();
            var userProfileState = userState.CreateProperty<UserProfileState>(nameof(UserProfileState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                if (includeUserProfile)
                {
                    await userProfileState.SetAsync(context, TestUserProfileState);
                    await userState.SaveChangesAsync(context);
                }

                var bot = sp.GetService<IBot>();
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }
    }
}
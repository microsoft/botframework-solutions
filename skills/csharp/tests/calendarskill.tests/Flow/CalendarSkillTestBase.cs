// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CalendarSkill.Bots;
using CalendarSkill.Dialogs;
using CalendarSkill.Models;
using CalendarSkill.Responses.ChangeEventStatus;
using CalendarSkill.Responses.CheckPersonAvailable;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.FindContact;
using CalendarSkill.Responses.JoinEvent;
using CalendarSkill.Responses.Main;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Responses.TimeRemaining;
using CalendarSkill.Responses.UpcomingEvent;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Services;
using CalendarSkill.Test.Flow.Fakes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkill.Test.Flow
{
    public class CalendarSkillTestBase : BotTestBase
    {
        public static readonly string Provider = "Azure Active Directory v2";

        public IServiceCollection Services { get; set; }

        public IStatePropertyAccessor<CalendarSkillState> CalendarStateAccessor { get; set; }

        public IServiceManager ServiceManager { get; set; }

        public ISearchService SearchService { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            this.ServiceManager = MockServiceManager.GetCalendarService();
            this.SearchService = new MockSearchClient();

            // Initialize service collection
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings()
            {
                OAuthConnections = new List<OAuthConnection>()
                {
                    new OAuthConnection() { Name = Provider, Provider = Provider }
                },

                AzureSearch = new BotSettings.AzureSearchConfiguration()
                {
                    SearchServiceName = "mockSearchService"
                }
            });

            Services.AddSingleton(new BotServices());
            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            Services.AddSingleton(new UserState(new MemoryStorage()));
            Services.AddSingleton(new ConversationState(new MemoryStorage()));
            Services.AddSingleton(new ProactiveState(new MemoryStorage()));
            Services.AddSingleton(new MicrosoftAppCredentials(string.Empty, string.Empty));
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                var proactiveState = sp.GetService<ProactiveState>();
                return new BotStateSet(userState, conversationState);
            });

            Services.AddSingleton<TestAdapter>(sp =>
            {
                var adapter = new DefaultTestAdapter();
                adapter.AddUserToken("Azure Active Directory v2", Channels.Test, "user1", "test");
                return adapter;
            });

            // Configure localized responses
            var supportedLocales = new List<string>() { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" };
            var templateFiles = new Dictionary<string, string>
            {
                { "ChangeEventStatus", "ChangeEventStatusDialogActivities" },
                { "CheckPersonAvailable", "CheckPersonAvailableActivities" },
                { "CreateEvent", "CreateEventDialogActivities" },
                { "FindContact", "FindContactDialogActivities" },
                { "JoinEvent", "JoinEventDialogActivities" },
                { "Main", "MainDialogActivities" },
                { "Shared", "SharedActivities" },
                { "Summary", "SummaryDialogActivities" },
                { "TimeRemaining", "TimeRemainingDialogActivities" },
                { "UpcomingEvent", "UpcomingEventDialogActivities" },
                { "UpdateEvent", "UpdateEventDialogActivities" },
                { "FindMeetingRoom", "FindMeetingRoomDialogActivities" },
            };

            var localizedTemplates = new Dictionary<string, List<string>>();
            foreach (var locale in supportedLocales)
            {
                var localeTemplateFiles = new List<string>();
                foreach (var (dialog, template) in templateFiles)
                {
                    // LG template for default locale should not include locale in file extension.
                    if (locale.Equals("en-us"))
                    {
                        localeTemplateFiles.Add(Path.Combine(".", "Responses", dialog, $"{template}.lg"));
                    }
                    else
                    {
                        localeTemplateFiles.Add(Path.Combine(".", "Responses", dialog, $"{template}.{locale}.lg"));
                    }
                }

                localizedTemplates.Add(locale, localeTemplateFiles);
            }

            Services.AddSingleton(new LocaleTemplateEngineManager(localizedTemplates, "en-us"));
            Services.AddSingleton(SearchService);

            // Configure files for generating all responses. Response from bot should equal one of them.
            var templateFilesAll = new List<string>()
            {
                @"ChangeEventStatus/ChangeEventStatusDialogTexts.lg",
                @"CheckPersonAvailable/CheckPersonAvailableTexts.lg",
                @"CreateEvent/CreateEventDialogTexts.lg",
                @"FindContact/FindContactDialogTexts.lg",
                @"JoinEvent/JoinEventDialogTexts.lg",
                @"Main/MainDialogTexts.lg",
                @"Shared/SharedTexts.lg",
                @"Summary/SummaryDialogTexts.lg",
                @"TimeRemaining/TimeRemainingDialogTexts.lg",
                @"UpcomingEvent/UpcomingEventDialogTexts.lg",
                @"UpdateEvent/UpdateEventDialogTexts.lg",
                @"FindMeetingRoom/FindMeetingRoomDialogTexts.lg",
            };

            var templatesAll = new List<string>();
            templateFilesAll.ForEach(s => templatesAll.Add(Path.Combine(".", "Responses", s)));
            var engineAll = new TemplateEngine().AddFiles(templatesAll);
            Services.AddSingleton(engineAll);

            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddSingleton(ServiceManager);
            Services.AddTransient<MainDialog>();
            Services.AddTransient<ChangeEventStatusDialog>();
            Services.AddTransient<JoinEventDialog>();
            Services.AddTransient<CreateEventDialog>();
            Services.AddTransient<FindContactDialog>();
            Services.AddTransient<ShowEventsDialog>();
            Services.AddTransient<TimeRemainingDialog>();
            Services.AddTransient<UpcomingEventDialog>();
            Services.AddTransient<UpdateEventDialog>();
            Services.AddTransient<CheckPersonAvailableDialog>();
            Services.AddTransient<FindMeetingRoomDialog>();
            Services.AddTransient<BookMeetingRoomDialog>();
            Services.AddTransient<UpdateMeetingRoomDialog>();
            Services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();

            var state = Services.BuildServiceProvider().GetService<ConversationState>();
            CalendarStateAccessor = state.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));

            TypeFactory.Configuration = new ConfigurationBuilder().Build();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.ServiceManager = MockServiceManager.SetAllToDefault();
            MockSearchClient.SetAllToDefault();
        }

        public string[] GetTemplates(string templateName, object data = null)
        {
            var sp = Services.BuildServiceProvider();
            var engine = sp.GetService<TemplateEngine>();
            var formatTemplateName = templateName + ".Text";
            return engine.ExpandTemplate(formatTemplateName, data).ToArray();
        }

        public TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            adapter.AddUserToken(Provider, Channels.Test, adapter.Conversation.User.Id, "test");

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                var state = await CalendarStateAccessor.GetAsync(context, () => new CalendarSkillState());
                state.EventSource = EventSource.Microsoft;
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }
    }
}

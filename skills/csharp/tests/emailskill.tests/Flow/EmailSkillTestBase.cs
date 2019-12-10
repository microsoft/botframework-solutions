// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using EmailSkill.Bots;
using EmailSkill.Dialogs;
using EmailSkill.Models;
using EmailSkill.Responses.DeleteEmail;
using EmailSkill.Responses.FindContact;
using EmailSkill.Responses.ForwardEmail;
using EmailSkill.Responses.Main;
using EmailSkill.Responses.ReplyEmail;
using EmailSkill.Responses.SendEmail;
using EmailSkill.Responses.Shared;
using EmailSkill.Responses.ShowEmail;
using EmailSkill.Services;
using EmailSkill.Tests.Flow.Fakes;
using EmailSkill.Tests.Flow.Utterances;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkill.Tests.Flow
{
    public class EmailSkillTestBase : BotTestBase
    {
        public static readonly string Provider = "Azure Active Directory v2";

        public IServiceCollection Services { get; set; }

        public MockServiceManager ServiceManager { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            // Initialize mock service manager
            ServiceManager = new MockServiceManager();

            // Initialize service collection
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings()
            {
                OAuthConnections = new List<OAuthConnection>()
                {
                    new OAuthConnection() { Name = Provider, Provider = Provider }
                }
            });

            Services.AddSingleton(new BotServices()
            {
                CognitiveModelSets = new Dictionary<string, CognitiveModelSet>
                {
                    {
                        "en-us", new CognitiveModelSet()
                        {
                            LuisServices = new Dictionary<string, LuisRecognizer>
                            {
                                { "General", new MockGeneralLuisRecognizer() },
                                {
                                    "Email", new MockEmailLuisRecognizer(
                                        new ForwardEmailUtterances(),
                                        new ReplyEmailUtterances(),
                                        new DeleteEmailUtterances(),
                                        new SendEmailUtterances(),
                                        new ShowEmailUtterances())
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
                { "DeleteEmail", "DeleteEmailActivities" },
                { "FindContact", "FindContactActivities" },
                { "Main", "MainDialogActivities" },
                { "SendEmail", "SendEmailActivities" },
                { "Shared", "SharedActivities" },
                { "ShowEmail", "ShowEmailActivities" }
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

            // Configure files for generating all responses. Response from bot should equal one of them.
            var templateFilesAll = new List<string>()
            {
                @"DeleteEmail\DeleteEmailTexts.lg",
                @"FindContact\FindContactTexts.lg",
                @"Main\MainDialogTexts.lg",
                @"SendEmail\SendEmailTexts.lg",
                @"Shared\SharedTexts.lg",
                @"ShowEmail\ShowEmailTexts.lg",
            };

            var templatesAll = new List<string>();
            templateFilesAll.ForEach(s => templatesAll.Add(Path.Combine(".", "Responses", s)));
            var engineAll = new TemplateEngine().AddFiles(templatesAll);
            Services.AddSingleton(engineAll);

            Services.AddSingleton<IStorage>(new MemoryStorage());

            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddSingleton<IServiceManager>(ServiceManager);

            Services.AddTransient<MainDialog>();
            Services.AddTransient<DeleteEmailDialog>();
            Services.AddTransient<FindContactDialog>();
            Services.AddTransient<ForwardEmailDialog>();
            Services.AddTransient<ReplyEmailDialog>();
            Services.AddTransient<SendEmailDialog>();
            Services.AddTransient<ShowEmailDialog>();
            Services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();

            ConfigData.GetInstance().MaxDisplaySize = 3;
            ConfigData.GetInstance().MaxReadSize = 3;
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

            var conversationState = sp.GetService<ConversationState>();
            var stateAccessor = conversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                var state = await stateAccessor.GetAsync(context, () => new EmailSkillState());
                state.MailSourceType = MailSource.Microsoft;
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }
    }
}
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
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
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
                        "en", new CognitiveModelSet()
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

                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                adapter.UseState(userState, conversationState);

                var resource = sp.GetService<ResourceExplorer>();
                adapter.UseResourceExplorer(resource);
                adapter.UseLanguageGeneration(resource, "ResponsesAndTexts.lg");

                adapter.AddUserToken("Azure Active Directory v2", Channels.Test, "user1", "test");

                return adapter;
            });

            var projPath = Environment.CurrentDirectory + @"\..\..\..\..\..\emailskill";
            var templateFiles = new List<string>()
            {
                @"DeleteEmail\DeleteEmailTexts.lg",
                @"FindContact\FindContactTexts.lg",
                @"Main\MainDialogTexts.lg",
                @"SendEmail\SendEmailTexts.lg",
                @"Shared\SharedTexts.lg",
                @"ShowEmail\ShowEmailTexts.lg",
            };
            var templates = new List<string>();
            templateFiles.ForEach(s => templates.Add(Path.Combine(projPath, "Responses", s)));
            var engine = new TemplateEngine().AddFiles(templates);
            Services.AddSingleton(engine);

            var resourceExplorer = ResourceExplorer.LoadProject(projPath);
            Services.AddSingleton(resourceExplorer);

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

            TypeFactory.Configuration = new ConfigurationBuilder().Build();
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
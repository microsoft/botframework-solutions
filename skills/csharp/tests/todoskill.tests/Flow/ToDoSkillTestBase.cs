// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Http;
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
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Bots;
using ToDoSkill.Dialogs;
using ToDoSkill.Responses.AddToDo;
using ToDoSkill.Responses.DeleteToDo;
using ToDoSkill.Responses.Main;
using ToDoSkill.Responses.MarkToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Responses.ShowToDo;
using ToDoSkill.Services;
using ToDoSkill.Tests.Flow.Fakes;
using ToDoSkill.Tests.Flow.Utterances;

namespace ToDoSkill.Tests.Flow
{
    public class ToDoSkillTestBase : BotTestBase
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
                                { MockData.LuisGeneral, new MockLuisRecognizer(new GeneralTestUtterances()) },
                                {
                                    MockData.LuisToDo, new MockLuisRecognizer(
                                    new DeleteToDoFlowTestUtterances(),
                                    new AddToDoFlowTestUtterances(),
                                    new MarkToDoFlowTestUtterances(),
                                    new ShowToDoFlowTestUtterances())
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

            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddSingleton<IServiceManager>(ServiceManager);

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

            Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            Services.AddTransient<MainDialog>();
            Services.AddTransient<AddToDoItemDialog>();
            Services.AddTransient<DeleteToDoItemDialog>();
            Services.AddTransient<MarkToDoItemDialog>();
            Services.AddTransient<ShowToDoItemDialog>();
            Services.AddTransient<IBot, DefaultActivityHandler<MainDialog>>();

            var templateFiles = new List<string>()
            {
                @"AddToDo\AddToDoTexts.lg",
                @"DeleteToDo\DeleteToDoTexts.lg",
                @"Main\ToDoMainTexts.lg",
                @"MarkToDo\MarkToDoTexts.lg",
                @"Shared\ToDoSharedTexts.lg",
                @"ShowToDo\ShowToDoTexts.lg",
            };
            var templates = new List<string>();
            templateFiles.ForEach(s => templates.Add(Path.Combine(Environment.CurrentDirectory, "Responses", s)));
            var engine = new TemplateEngine().AddFiles(templates);
            Services.AddSingleton(engine);

            var projPath = Environment.CurrentDirectory.Substring(0, Environment.CurrentDirectory.IndexOf("bin"));
            var resourceExplorer = ResourceExplorer.LoadProject(projPath);
            Services.AddSingleton(resourceExplorer);

            Services.AddSingleton<IStorage>(new MemoryStorage());

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

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }
    }
}
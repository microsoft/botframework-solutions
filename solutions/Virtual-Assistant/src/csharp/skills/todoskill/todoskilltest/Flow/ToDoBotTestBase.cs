// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Autofac;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Models.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill;
using ToDoSkill.Dialogs.AddToDo.Resources;
using ToDoSkill.Dialogs.DeleteToDo.Resources;
using ToDoSkill.Dialogs.Main.Resources;
using ToDoSkill.Dialogs.MarkToDo.Resources;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.Dialogs.ShowToDo.Resources;
using ToDoSkill.ServiceClients;
using ToDoSkillTest.Flow.Fakes;

namespace ToDoSkillTest.Flow
{
    public class ToDoBotTestBase : BotTestBase
    {
        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public ProactiveState ProactiveState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public IBackgroundTaskQueue BackgroundTaskQueue { get; set; }

        public EndpointService EndpointService { get; set; }

        public IStatePropertyAccessor<ToDoSkillState> ToDoStateAccessor { get; set; }

        public IStatePropertyAccessor<ToDoSkillUserState> UserStateAccessor { get; set; }

        public IServiceManager ServiceManager { get; set; }

        public MockSkillConfiguration Services { get; set; }

        public BotConfiguration Options { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            var builder = new ContainerBuilder();

            this.ConversationState = new ConversationState(new MemoryStorage());
            this.UserState = new UserState(new MemoryStorage());
            this.ProactiveState = new ProactiveState(new MemoryStorage());
            this.TelemetryClient = new NullBotTelemetryClient();
            this.BackgroundTaskQueue = new BackgroundTaskQueue();
            this.ToDoStateAccessor = this.ConversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));
            this.UserStateAccessor = this.UserState.CreateProperty<ToDoSkillUserState>(nameof(ToDoSkillUserState));
            this.Services = new MockSkillConfiguration();
            this.EndpointService = new EndpointService();

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));
            var fakeServiceManager = new MockServiceManager();
            builder.RegisterInstance<IServiceManager>(fakeServiceManager);

            this.Container = builder.Build();
            this.ServiceManager = fakeServiceManager;

            ResponseManager = new ResponseManager(
                responseTemplates: new IResponseIdCollection[]
                {
                    new AddToDoResponses(),
                    new DeleteToDoResponses(),
                    new ToDoMainResponses(),
                    new MarkToDoResponses(),
                    new ToDoSharedResponses(),
                    new ShowToDoResponses(),
                },
                locales: new string[] { "en", "de", "es", "fr", "it", "zh" });
        }

        public Activity GetAuthResponse()
        {
            var providerTokenResponse = new ProviderTokenResponse
            {
                TokenResponse = new TokenResponse(token: "test")
            };
            return new Activity(ActivityTypes.Event, name: "tokens/response", value: providerTokenResponse);
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(this.ConversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = this.BuildBot() as ToDoSkill.ToDoSkill;
                var state = await this.ToDoStateAccessor.GetAsync(context, () => new ToDoSkillState());
                var userState = await this.UserStateAccessor.GetAsync(context, () => new ToDoSkillUserState());
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new ToDoSkill.ToDoSkill(Services, EndpointService, ConversationState, UserState, ProactiveState, TelemetryClient, BackgroundTaskQueue, true, ResponseManager, ServiceManager);
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Autofac;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill;
using ToDoSkill.ServiceClients;
using ToDoSkillTest.Flow.Fakes;

namespace ToDoSkillTest.Flow
{
    public class ToDoBotTestBase : BotTestBase
    {
        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

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
            this.TelemetryClient = new NullBotTelemetryClient();
            this.ToDoStateAccessor = this.ConversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));
            this.UserStateAccessor = this.UserState.CreateProperty<ToDoSkillUserState>(nameof(ToDoSkillUserState));
            this.Services = new MockSkillConfiguration();

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));
            var fakeServiceManager = new MockServiceManager();
            builder.RegisterInstance<IServiceManager>(fakeServiceManager);

            this.Container = builder.Build();
            this.ServiceManager = fakeServiceManager;

            this.BotResponseBuilder = new BotResponseBuilder();
            this.BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());
        }

        public Activity GetAuthResponse()
        {
            ProviderTokenResponse providerTokenResponse = new ProviderTokenResponse();
            providerTokenResponse.TokenResponse = new TokenResponse(token: "test");
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
            return new ToDoSkill.ToDoSkill(this.Services, this.ConversationState, this.UserState, this.TelemetryClient, this.ServiceManager, true);
        }
    }
}
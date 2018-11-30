// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading;
using Autofac;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestFramework;
using ToDoSkill;
using ToDoSkillTest.Fakes;
using ToDoSkillTest.Flow.Fakes;

namespace ToDoSkillTest.Flow
{
    public class ToDoBotTestBase : BotTestBase
    {
        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public IStatePropertyAccessor<ToDoSkillState> ToDoStateAccessor { get; set; }

        public ITaskService ToDoService { get; set; }

        public MockSkillConfiguration Services { get; set; }

        public BotConfiguration Options { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            var builder = new ContainerBuilder();

            this.ConversationState = new ConversationState(new MemoryStorage());
            this.UserState = new UserState(new MemoryStorage());
            this.ToDoStateAccessor = this.ConversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));
            this.Services = new MockSkillConfiguration();

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));
            var fakeToDoService = new FakeToDoService();
            builder.RegisterInstance<ITaskService>(fakeToDoService);

            this.Container = builder.Build();
            this.ToDoService = fakeToDoService;

            this.BotResponseBuilder = new BotResponseBuilder();
            this.BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(this.ConversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = this.BuildBot() as ToDoSkill.ToDoSkill;
                var state = await this.ToDoStateAccessor.GetAsync(context, () => new ToDoSkillState());
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new ToDoSkill.ToDoSkill(this.Services, this.ConversationState, this.UserState, this.ToDoService);
        }
    }
}

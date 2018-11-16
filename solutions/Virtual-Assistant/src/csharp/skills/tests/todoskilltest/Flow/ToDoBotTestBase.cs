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
        public ConversationState conversationState { get; set; }

        public UserState userState { get; set; }

        public IStatePropertyAccessor<ToDoSkillState> toDoStateAccessor;

        public ITaskService toDoService { get; set; }

        public MockSkillConfiguration services { get; set; }

        public BotConfiguration options { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            var builder = new ContainerBuilder();

            this.conversationState = new ConversationState(new MemoryStorage());
            this.userState = new UserState(new MemoryStorage());
            this.toDoStateAccessor = this.conversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));
            this.services = new MockSkillConfiguration();

            builder.RegisterInstance(new BotStateSet(this.userState, this.conversationState));
            var fakeToDoService = new FakeToDoService();
            builder.RegisterInstance<ITaskService>(fakeToDoService);

            this.Container = builder.Build();
            this.toDoService = fakeToDoService;

            this.BotResponseBuilder = new BotResponseBuilder();
            this.BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(this.conversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = this.BuildBot() as ToDoSkill.ToDoSkill;
                var state = await this.toDoStateAccessor.GetAsync(context, () => new ToDoSkillState());
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new ToDoSkill.ToDoSkill(this.services, this.conversationState, this.userState, this.toDoService);
        }
    }
}

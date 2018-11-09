// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkillTest.Flow
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Adapters;
    using Microsoft.Extensions.Configuration;
    using System.Threading;
    using TestFramework;
    using ToDoSkill;
    using Autofac;
    using ToDoSkillTest.Fakes;
    using ToDoSkillTest.Flow.Fakes;
    using Microsoft.Bot.Solutions.Skills;
    using Microsoft.Bot.Configuration;
    using System.Collections.Generic;
    using Microsoft.Bot.Solutions.Dialogs;
    using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
    
    public class ToDoBotTestBase : BotTestBase
    {

        public ConversationState conversationState { get; set; }

        public UserState userState { get; set; }

        public IStatePropertyAccessor<ToDoSkillState> toDoStateAccessor;

        public ITaskService toDoService { get; set; }

        public MockSkillConfiguration services { get; set; }

        public BotConfiguration options { get; set; }

        public override void Initialize()
        {
            this.Configuration = new BuildConfig().Configuration;
            var builder = new ContainerBuilder();
            builder.RegisterInstance<IConfiguration>(this.Configuration);
            var botFilePath = this.Configuration.GetSection("botFilePath")?.Value;
            var botFileSecret = this.Configuration.GetSection("botFileSecret")?.Value;
            var options = BotConfiguration.Load(botFilePath ?? @".\ToDoSkillTest.bot", botFileSecret);
            builder.RegisterInstance(options);

            this.conversationState = new ConversationState(new MemoryStorage());
            this.userState = new UserState(new MemoryStorage());
            this.toDoStateAccessor = this.conversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));

            var parameters = this.Configuration.GetSection("Parameters")?.Get<string[]>();
            var configuration = this.Configuration.GetSection("Configuration")?.Get<Dictionary<string, object>>();
            this.services = new MockSkillConfiguration();

            builder.RegisterInstance(new BotStateSet(this.userState, this.conversationState));
            var fakeToDoService = new FakeToDoService();
            builder.RegisterInstance<ITaskService>(fakeToDoService);

            this.Container = builder.Build();
            this.toDoService = fakeToDoService;
            this.options = options;

            this.BotResponseBuilder = new BotResponseBuilder();
            this.BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(this.conversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = this.BuildBot() as ToDoSkill;
                var state = await this.toDoStateAccessor.GetAsync(context, () => new ToDoSkillState());
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new ToDoSkill(this.services, this.conversationState, this.userState, this.toDoService);
        }
    }
}

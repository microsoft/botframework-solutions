// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkillTest.Flow
{
    using System.Threading;
    using Autofac;
    using CalendarSkill;
    using CalendarSkillTest.Fakes;
    using Microsoft.Bot.Configuration;
    using Microsoft.Bot.Solutions.Skills;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Adapters;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using TestFramework;
    using TestFramework.Fakes;
    using Microsoft.Bot.Solutions.Dialogs;
    using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
    using System.Collections.Generic;

    public class CalendarBotTestBase : BotTestBase
    {
        public SkillConfiguration Services;

        public ConversationState ConversationState { get; set; }

        public IStatePropertyAccessor<CalendarSkillState> Accessors { get; set; }

        public IServiceManager ServiceManager { get; set; }

        public UserState UserState;

        public BotConfiguration Options { get; set; }

        public override void Initialize()
        {
            this.Configuration = new BuildConfig().Configuration;
            var builder = new ContainerBuilder();
            builder.RegisterInstance<IConfiguration>(this.Configuration);
            var options = BotConfiguration.LoadAsync("CalendarSkillTest.bot").GetAwaiter().GetResult();
            builder.RegisterInstance(options);

            var conversationState = new ConversationState(new MemoryStorage());
            var accessors = conversationState.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));

            builder.RegisterInstance(accessors);
            var fakeServiceManager = new FakeCalendarServiceManager();
            builder.RegisterInstance<IServiceManager>(fakeServiceManager);

            this.Container = builder.Build();
            this.Accessors = accessors;
            this.ServiceManager = fakeServiceManager;
            this.Options = options;
            this.ConversationState = conversationState;

            this.UserState = new UserState(new MemoryStorage());

            this.BotResponseBuilder = new BotResponseBuilder();
            this.BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());

            var parameters = this.Configuration.GetSection("Parameters")?.Get<string[]>();
            var configuration = this.Configuration.GetSection("Configuration")?.Get<Dictionary<string, object>>();
            this.Services = new SkillConfiguration(options, parameters, configuration);
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(this.ConversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = this.BuildBot() as CalendarSkill;
                var state = await Accessors.GetAsync(context, () => new CalendarSkillState());
                state.APIToken =
                    "test token";
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new CalendarSkill(Services, ConversationState, UserState, ServiceManager);
        }
    }
}

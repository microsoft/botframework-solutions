using System.Collections.Generic;
using System.Threading;
using Autofac;
using CalendarSkill;
using CalendarSkillTest.Flow.Fakes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Extensions.Configuration;
using TestFramework;

namespace CalendarSkillTest.Flow
{
    public class CalendarBotTestBase : BotTestBase
    {
        public IStatePropertyAccessor<CalendarSkillState> CalendarStateAccessor { get; set; }

        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public IServiceManager ServiceManager { get; set; }

        public SkillConfiguration Services { get; set; }

        public BotConfiguration Options { get; set; }

        public override void Initialize()
        {
            this.Configuration = new BuildConfig().Configuration;
            var builder = new ContainerBuilder();
            builder.RegisterInstance<IConfiguration>(this.Configuration);
            var botFilePath = this.Configuration.GetSection("botFilePath")?.Value;
            var botFileSecret = this.Configuration.GetSection("botFileSecret")?.Value;
            var options = BotConfiguration.Load(botFilePath ?? @".\CalendarSkillTest.bot", botFileSecret);
            builder.RegisterInstance(options);

            this.ConversationState = new ConversationState(new MemoryStorage());
            this.UserState = new UserState(new MemoryStorage());
            this.CalendarStateAccessor = this.ConversationState.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));

            var parameters = this.Configuration.GetSection("Parameters")?.Get<string[]>();
            var configuration = this.Configuration.GetSection("Configuration")?.Get<Dictionary<string, object>>();

            this.Services = new MockSkillConfiguration();
            //this.Services.LuisServices.Add("general", new MockLuisRecognizer());
            //this.Services.LuisServices.Add("calendar", new MockLuisRecognizer());
            //this.Services.AuthConnectionName = "test";
            //this.Services.TelemetryClient = null;
            //this.Services.CosmosDbOptions = null;

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));
            var fakeServiceManager = new MockCalendarServiceManager();
            builder.RegisterInstance<IServiceManager>(fakeServiceManager);

            this.Container = builder.Build();
            this.ServiceManager = fakeServiceManager;
            this.Options = options;

            this.BotResponseBuilder = new BotResponseBuilder();
            this.BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(this.ConversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = this.BuildBot() as CalendarSkill.CalendarSkill;
                var state = await this.CalendarStateAccessor.GetAsync(context, () => new CalendarSkillState());
                state.APIToken = "test";
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new CalendarSkill.CalendarSkill(this.Services, this.ConversationState, this.UserState,  this.ServiceManager);
        }
    }
}

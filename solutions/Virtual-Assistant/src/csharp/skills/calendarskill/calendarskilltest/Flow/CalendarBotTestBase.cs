using System.Threading;
using Autofac;
using CalendarSkill;
using CalendarSkill.Dialogs.ChangeEventStatus.Resources;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using CalendarSkill.Dialogs.JoinEvent.Resources;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.Summary.Resources;
using CalendarSkill.Dialogs.TimeRemaining.Resources;
using CalendarSkill.Dialogs.UpdateEvent.Resources;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkillTest.Flow.Fakes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    public class CalendarBotTestBase : BotTestBase
    {
        public IStatePropertyAccessor<CalendarSkillState> CalendarStateAccessor { get; set; }

        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public IServiceManager ServiceManager { get; set; }

        public SkillConfiguration Services { get; set; }

        public BotConfiguration Options { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            var builder = new ContainerBuilder();

            this.ConversationState = new ConversationState(new MemoryStorage());
            this.UserState = new UserState(new MemoryStorage());
            this.TelemetryClient = new NullBotTelemetryClient();
            this.CalendarStateAccessor = this.ConversationState.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));
            this.Services = new MockSkillConfiguration();

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));
            var fakeServiceManager = new MockCalendarServiceManager();
            builder.RegisterInstance<IServiceManager>(fakeServiceManager);

            this.Container = builder.Build();
            this.ServiceManager = fakeServiceManager;

            ResponseManager = new ResponseManager(
                responseTemplates: new IResponseIdCollection[]
                {
                    new ChangeEventStatusResponses(),
                    new CreateEventResponses(),
                    new JoinEventResponses(),
                    new CalendarMainResponses(),
                    new CalendarSharedResponses(),
                    new SummaryResponses(),
                    new TimeRemainingResponses(),
                    new UpdateEventResponses(),
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
                var bot = this.BuildBot() as CalendarSkill.CalendarSkill;
                var state = await CalendarStateAccessor.GetAsync(context, () => new CalendarSkillState());
                state.APIToken = "test";
                state.EventSource = EventSource.Microsoft;
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new CalendarSkill.CalendarSkill(Services, ConversationState, UserState, TelemetryClient, ResponseManager, ServiceManager, true);
        }
    }
}

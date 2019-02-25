using System.Threading;
using Autofac;
using CalendarSkill;
using CalendarSkill.Dialogs.ChangeEventStatus.Resources;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using CalendarSkill.Dialogs.FindContact.Resources;
using CalendarSkill.Dialogs.JoinEvent.Resources;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.Summary.Resources;
using CalendarSkill.Dialogs.TimeRemaining.Resources;
using CalendarSkill.Dialogs.UpcomingEvent.Resources;
using CalendarSkill.Dialogs.UpdateEvent.Resources;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkillTest.Flow.Fakes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Models.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    public class CalendarBotTestBase : BotTestBase
    {
        public IStatePropertyAccessor<CalendarSkillState> CalendarStateAccessor { get; set; }

        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public ProactiveState ProactiveState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public IBackgroundTaskQueue BackgroundTaskQueue { get; set; }

        public IServiceManager ServiceManager { get; set; }

        public SkillConfiguration Services { get; set; }

        public EndpointService EndpointService { get; set; }

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
            this.CalendarStateAccessor = this.ConversationState.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));
            this.Services = new MockSkillConfiguration();
            this.EndpointService = new EndpointService();

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));

            this.Container = builder.Build();
            this.ServiceManager = MockServiceManager.GetCalendarService();

            ResponseManager = new ResponseManager(
                responseTemplates: new IResponseIdCollection[]
                {
                    new FindContactResponses(),
                    new ChangeEventStatusResponses(),
                    new CreateEventResponses(),
                    new JoinEventResponses(),
                    new CalendarMainResponses(),
                    new CalendarSharedResponses(),
                    new SummaryResponses(),
                    new TimeRemainingResponses(),
                    new UpdateEventResponses(),
                    new UpcomingEventResponses()
                },
                locales: new string[] { "en", "de", "es", "fr", "it", "zh" });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.ServiceManager = MockServiceManager.SetAllToDefault();
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
            return new CalendarSkill.CalendarSkill(this.Services, this.EndpointService, this.ConversationState, this.UserState, this.ProactiveState, this.TelemetryClient, this.BackgroundTaskQueue, true, ResponseManager, this.ServiceManager);
        }
    }
}

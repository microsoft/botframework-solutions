using System.Collections.Generic;
using System.Threading;
using CalendarSkill.Bots;
using CalendarSkill.Dialogs;
using CalendarSkill.Models;
using CalendarSkill.Responses.ChangeEventStatus;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.FindContact;
using CalendarSkill.Responses.JoinEvent;
using CalendarSkill.Responses.Main;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Responses.TimeRemaining;
using CalendarSkill.Responses.UpcomingEvent;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Services;
using CalendarSkillTest.Flow.Fakes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    public class CalendarBotTestBase : BotTestBase
    {
        public IServiceCollection Services { get; set; }

        public IStatePropertyAccessor<CalendarSkillState> CalendarStateAccessor { get; set; }

        public IServiceManager ServiceManager { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            this.ServiceManager = MockServiceManager.GetCalendarService();

            // Initialize service collection
            Services = new ServiceCollection();
            Services.AddSingleton(new BotSettings()
            {
                OAuthConnections = new List<OAuthConnection>()
                {
                    new OAuthConnection() { Name = "Microsoft", Provider = "Microsoft" }
                }
            });

            Services.AddSingleton(new BotServices());
            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            Services.AddSingleton(new UserState(new MemoryStorage()));
            Services.AddSingleton(new ConversationState(new MemoryStorage()));
            Services.AddSingleton(new ProactiveState(new MemoryStorage()));
            Services.AddSingleton(sp =>
            {
                var userState = sp.GetService<UserState>();
                var conversationState = sp.GetService<ConversationState>();
                var proactiveState = sp.GetService<ProactiveState>();
                return new BotStateSet(userState, conversationState);
            });

            ResponseManager = new ResponseManager(
                new string[] { "en", "de", "es", "fr", "it", "zh" },
                new FindContactResponses(),
                new ChangeEventStatusResponses(),
                new CreateEventResponses(),
                new JoinEventResponses(),
                new CalendarMainResponses(),
                new CalendarSharedResponses(),
                new SummaryResponses(),
                new TimeRemainingResponses(),
                new UpdateEventResponses(),
                new UpcomingEventResponses());
            Services.AddSingleton(ResponseManager);

            Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            Services.AddSingleton(ServiceManager);
            Services.AddSingleton<TestAdapter, DefaultTestAdapter>();
            Services.AddTransient<MainDialog>();
			Services.AddTransient<ChangeEventStatusDialog>();
			Services.AddTransient<ConnectToMeetingDialog>();
			Services.AddTransient<CreateEventDialog>();
			Services.AddTransient<FindContactDialog>();
			Services.AddTransient<SummaryDialog>();
			Services.AddTransient<TimeRemainingDialog>();
			Services.AddTransient<UpcomingEventDialog>();
			Services.AddTransient<UpdateEventDialog>();
			Services.AddTransient<FindContactDialog>();
			Services.AddTransient<IBot, DialogBot<MainDialog>>();

            var state = Services.BuildServiceProvider().GetService<ConversationState>();
            CalendarStateAccessor = state.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));
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
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                var state = await CalendarStateAccessor.GetAsync(context, () => new CalendarSkillState());
                state.APIToken = "test";
                state.EventSource = EventSource.Microsoft;
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }
    }
}
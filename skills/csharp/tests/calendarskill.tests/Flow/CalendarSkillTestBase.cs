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
using CalendarSkill.Test.Flow.Fakes;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;

namespace CalendarSkill.Test.Flow
{
    public class CalendarSkillTestBase : BotTestBase
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
                    new OAuthConnection() { Name = "Azure Active Directory v2", Provider = "Azure Active Directory v2" }
                }
            });

            Services.AddSingleton(new BotServices());
            Services.AddSingleton<IBotTelemetryClient, NullBotTelemetryClient>();
            Services.AddSingleton(new UserState(new MemoryStorage()));
            Services.AddSingleton(new ConversationState(new MemoryStorage()));
            Services.AddSingleton(new ProactiveState(new MemoryStorage()));
            Services.AddSingleton(new MicrosoftAppCredentials(string.Empty, string.Empty));
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
            Services.AddTransient<JoinEventDialog>();
            Services.AddTransient<CreateEventDialog>();
            Services.AddTransient<FindContactDialog>();
            Services.AddTransient<ShowEventsDialog>();
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

        public TestFlow GetTestFlow()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            adapter.AddUserToken("Azure Active Directory v2", Channels.Test, adapter.Conversation.User.Id, "test");

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = sp.GetService<IBot>();
                var state = await CalendarStateAccessor.GetAsync(context, () => new CalendarSkillState());
                state.EventSource = EventSource.Microsoft;
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }
    }
}

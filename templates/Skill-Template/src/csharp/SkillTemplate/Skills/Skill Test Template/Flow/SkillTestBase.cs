using Autofac;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Builder.Solutions.Testing;
using Microsoft.Bot.Builder.Solutions.Testing.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using $safeprojectname$.Flow.LuisTestUtils;
using $ext_safeprojectname$.Dialogs.Main.Resources;
using $ext_safeprojectname$.Dialogs.Sample.Resources;
using $ext_safeprojectname$.Dialogs.Shared.Resources;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace $safeprojectname$.Flow
{
    public class $ext_safeprojectname$TestBase : BotTestBase
    {
        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public ProactiveState ProactiveState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public IBackgroundTaskQueue BackgroundTaskQueue { get; set; }

        public SkillConfigurationBase Services { get; set; }

        public EndpointService EndpointService { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            var builder = new ContainerBuilder();

            ConversationState = new ConversationState(new MemoryStorage());
            UserState = new UserState(new MemoryStorage());
            this.ProactiveState = new ProactiveState(new MemoryStorage());
            this.TelemetryClient = new NullBotTelemetryClient();
            this.BackgroundTaskQueue = new BackgroundTaskQueue();
            this.Services = new MockSkillConfiguration();
            this.EndpointService = new EndpointService();

            Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
                {
                    { "general", GeneralTestUtil.CreateRecognizer() },
                    { "$ext_safeprojectname$", $ext_safeprojectname$TestUtil.CreateRecognizer() }
                }
            });

            builder.RegisterInstance(new BotStateSet(UserState, ConversationState));
            Container = builder.Build();

            ResponseManager = new ResponseManager(
                Services.LocaleConfigurations.Keys.ToArray(),
                new MainResponses(),
                new SharedResponses(),
                new SampleResponses());
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(ConversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = BuildBot() as $ext_safeprojectname$.$ext_safeprojectname$;
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new $ext_safeprojectname$.$ext_safeprojectname$(this.Services, this.EndpointService, this.ConversationState, this.UserState, this.ProactiveState, this.TelemetryClient, this.BackgroundTaskQueue, false, this.ResponseManager, null);
}
    }
}
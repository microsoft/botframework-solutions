using Autofac;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Bot.Solutions.Models.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PointOfInterestSkill.Dialogs.CancelRoute.Resources;
using PointOfInterestSkill.Dialogs.Route.Resources;
using PointOfInterestSkill.Dialogs.Shared.Resources;
using PointOfInterestSkill.ServiceClients;
using PointOfInterestSkillTests.API.Fakes;
using PointOfInterestSkillTests.Flow.Fakes;
using System.Threading;

namespace PointOfInterestSkillTests.Flow
{
    public class PointOfInterestTestBase : BotTestBase
    {
        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public SkillConfigurationBase Services { get; set; }

        public IServiceManager ServiceManager { get; set; }

        public EndpointService EndpointService { get; set; }

        public ProactiveState ProactiveState { get; set; }

        public IBackgroundTaskQueue BackgroundTaskQueue { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            var builder = new ContainerBuilder();

            ConversationState = new ConversationState(new MemoryStorage());
            UserState = new UserState(new MemoryStorage());
            TelemetryClient = new NullBotTelemetryClient();
            Services = new MockSkillConfiguration();

            builder.RegisterInstance(new BotStateSet(UserState, ConversationState));
            var fakeServiceManager = new MockServiceManager();
            builder.RegisterInstance<IServiceManager>(fakeServiceManager);

            this.Container = builder.Build();
            this.ServiceManager = fakeServiceManager;
            this.EndpointService = new EndpointService();
            this.BackgroundTaskQueue = new BackgroundTaskQueue();
            this.ProactiveState = new ProactiveState(new MemoryStorage());

            ResponseManager = new ResponseManager(
                responseTemplates: new IResponseIdCollection[]
                {
                    new POISharedResponses(),
                    new RouteResponses(),
                    new CancelRouteResponses()
                },
                locales: new string[] { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" });
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(ConversationState))
                .Use(new EventDebuggerMiddleware());

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = BuildBot() as PointOfInterestSkill.PointOfInterestSkill;
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new PointOfInterestSkill.PointOfInterestSkill(Services, EndpointService, ConversationState, UserState, ProactiveState, TelemetryClient, BackgroundTaskQueue, true, ResponseManager, ServiceManager);
        }
    }
}
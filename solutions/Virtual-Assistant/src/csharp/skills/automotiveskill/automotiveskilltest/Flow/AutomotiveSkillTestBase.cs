using System.Threading;
using Autofac;
using AutomotiveSkill;
using AutomotiveSkill.Dialogs.Main.Resources;
using AutomotiveSkill.Dialogs.Shared.Resources;
using AutomotiveSkill.Dialogs.VehicleSettings.Resources;
using AutomotiveSkillTest.Flow.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Models.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Testing;

namespace AutomotiveSkillTest.Flow
{
    public class AutomotiveSkillTestBase : BotTestBase
    {
        public IStatePropertyAccessor<AutomotiveSkillState> AutomotiveSkillStateAccessor { get; set; }

        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public ProactiveState ProactiveState { get; set; }

        public SkillConfigurationBase Services { get; set; }

        public BotConfiguration Options { get; set; }

        public HttpContext MockHttpContext { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public IBackgroundTaskQueue BackgroundTaskQueue { get; set; }

        public EndpointService EndpointService { get; set; }

        public HttpContextAccessor MockHttpContextAcessor { get; set; }

        public string ImageAssetLocation { get; set; }

        public override void Initialize()
        {
            var builder = new ContainerBuilder();

            ConversationState = new ConversationState(new MemoryStorage());
            UserState = new UserState(new MemoryStorage());
            ProactiveState = new ProactiveState(new MemoryStorage());
            AutomotiveSkillStateAccessor = ConversationState.CreateProperty<AutomotiveSkillState>(nameof(AutomotiveSkillState));
            Services = new MockSkillConfiguration();
            BackgroundTaskQueue = new BackgroundTaskQueue();
            EndpointService = new EndpointService();

            ResponseManager = new ResponseManager(
                responseTemplates: new IResponseIdCollection[] 
                {
                    new AutomotiveSkillMainResponses(),
                    new AutomotiveSkillSharedResponses(),
                    new VehicleSettingsResponses()
                },
                locales: new string[] { "en-us", "de-de", "es-es", "fr-fr", "it-it", "zh-cn" });
            ImageAssetLocation = "https://localhost";
            this.Services.Properties.Add("ImageAssetLocation", ImageAssetLocation);

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));

            builder.RegisterInstance(new BotStateSet(UserState, ConversationState));

            Container = builder.Build();

            TelemetryClient = new NullBotTelemetryClient();

            // Mock HttpContext for image path resolution
            MockHttpContext = new DefaultHttpContext();
            MockHttpContext.Request.Scheme = "http";
            MockHttpContext.Request.Host = new HostString("localhost", 3980);

            MockHttpContextAcessor = new HttpContextAccessor
            {
                HttpContext = MockHttpContext
            };
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(ConversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = BuildBot() as AutomotiveSkill.AutomotiveSkill;
                var state = await AutomotiveSkillStateAccessor.GetAsync(context, () => new AutomotiveSkillState());
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new AutomotiveSkill.AutomotiveSkill(Services, EndpointService, ConversationState, UserState, ProactiveState, TelemetryClient, BackgroundTaskQueue, true, ResponseManager, null, MockHttpContextAcessor);
        }
    }
}
using Autofac;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Bot.Solutions.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DemoSkillTests.Flow.LuisTestUtils;
using DemoSkill.Dialogs.Main.Resources;
using DemoSkill.Dialogs.Sample.Resources;
using DemoSkill.Dialogs.Shared.Resources;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DemoSkillTests.Flow
{
    public class DemoSkillTestBase : BotTestBase
    {
        public ConversationState ConversationState { get; set; }

        public UserState UserState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public SkillConfigurationBase Services { get; set; }

        [TestInitialize]
        public override void Initialize()
        {
            var builder = new ContainerBuilder();

            ConversationState = new ConversationState(new MemoryStorage());
            UserState = new UserState(new MemoryStorage());
            TelemetryClient = new NullBotTelemetryClient();
            Services = new MockSkillConfiguration();

            Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
                {
                    { "general", GeneralTestUtil.CreateRecognizer() },
                    { "DemoSkill", DemoSkillTestUtil.CreateRecognizer() }
                }
            });

            builder.RegisterInstance(new BotStateSet(UserState, ConversationState));
            Container = builder.Build();

            ResponseManager = new ResponseManager(
            new IResponseIdCollection[]
            {
                new MainResponses(),
                new SharedResponses(),
                new SampleResponses()
            }, Services.LocaleConfigurations.Keys.ToArray());
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(ConversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = BuildBot() as DemoSkill.DemoSkill;
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new DemoSkill.DemoSkill(Services, ConversationState, UserState, TelemetryClient, false, ResponseManager, null);
        }
    }
}
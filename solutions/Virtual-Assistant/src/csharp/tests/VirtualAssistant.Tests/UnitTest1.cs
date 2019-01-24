using Autofac;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using VirtualAssistant.Tests.LuisTestUtils;

namespace VirtualAssistant.Tests
{
    [TestClass]
    public class SkillTests : BotTestBase
    {
        private ConversationState _conversationState { get; set; }
        private IStatePropertyAccessor<DialogState> _dialogState { get; set; }
        private DialogSet _dialogs { get; set; }
        private UserState _userState { get; set; }
        private IBotTelemetryClient _telemetryClient { get; set; }
        private SkillConfigurationBase _services { get; set; }
        private SkillDialogOptions _skillDialogOptions { get; set; }
        private Dictionary<string, SkillConfigurationBase> _skills;

        [TestInitialize]
        public void Initialize()
        {
             var builder = new ContainerBuilder();
            _conversationState = new ConversationState(new MemoryStorage());
            _dialogState = _conversationState.CreateProperty<DialogState>(nameof(_dialogState));
            _userState = new UserState(new MemoryStorage());
            _telemetryClient = new NullBotTelemetryClient();

            // Add the LUIS model fakes used by the Virtual Assistant including the Dispatcher

            _services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
                {
                    { "dispatch", DispatchTestUtil.CreateRecognizer() },
                    { "general", GeneralTestUtil.CreateRecognizer() },
                    { "CalendarSkill", CalendarTestUtil.CreateRecognizer() }
                }
            });

            builder.RegisterInstance(new BotStateSet(_userState, _conversationState));
            Container = builder.Build();

            BotResponseBuilder = new BotResponseBuilder();
            BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());      

            const string calendarSkillName = "calendarSkill";
            var calendarSkillDefinition = new SkillDefinition();
            var fakeSkillType = typeof(CalendarSkill.CalendarSkill);
            calendarSkillDefinition.Assembly = fakeSkillType.AssemblyQualifiedName;
            calendarSkillDefinition.Id = calendarSkillName;
            calendarSkillDefinition.Name = calendarSkillName;

            _skills = new Dictionary<string, SkillConfigurationBase>
            {
                { calendarSkillDefinition.Id, _services }

            };
        }
        [TestMethod]
        public void TestMethod1()
        {

        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(_conversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = BuildBot() as VirtualAssistant;
                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new VirtualAssistant(null, null,_conversationState, _userState, null, _telemetryClient);
        }
    }
}

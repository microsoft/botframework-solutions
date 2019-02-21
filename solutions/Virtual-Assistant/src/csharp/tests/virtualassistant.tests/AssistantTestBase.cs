using System;
using System.Collections.Generic;
using System.Threading;
using Autofac;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Models.Proactive;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Bot.Solutions.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PointOfInterestSkill.Dialogs.Shared.Resources;
using VirtualAssistant.Tests.LuisTestUtils;

namespace VirtualAssistant.Tests
{
    /// <summary>
    /// Base class for Assistant tests which prepare common configuration such as the LUIS mocks and skill
    /// </summary>
    [TestClass]
    public class AssistantTestBase : BotTestBase
    {
        public UserState UserState { get; set; }

        public ConversationState ConversationState { get; set; }

        public ProactiveState ProactiveState { get; set; }

        public IBackgroundTaskQueue BackgroundTaskQueue { get; set; }

        public IStatePropertyAccessor<DialogState> DialogState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public EndpointService EndPointService { get; set; }

        public BotServices BotServices { get; set; }

        [TestInitialize]
        public new void Initialize()
        {
            var builder = new ContainerBuilder();

            this.ConversationState = new ConversationState(new MemoryStorage());
            this.DialogState = this.ConversationState.CreateProperty<DialogState>(nameof(this.DialogState));
            this.UserState = new UserState(new MemoryStorage());
            this.ProactiveState = new ProactiveState(new MemoryStorage());
            this.TelemetryClient = new NullBotTelemetryClient();
            this.BackgroundTaskQueue = new BackgroundTaskQueue();

            this.EndPointService = new EndpointService();

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));
            this.Container = builder.Build();

            ResponseManager = new ResponseManager(
                responseTemplates: new IResponseIdCollection[]
                {
                    // todo: register response files
                    new CommonResponses(),
                    new POISharedResponses()
                },
                locales: new string[] { "en", "de", "es", "fr", "it", "zh" });

            // Initialize the Dispatch and Luis mocks
            this.BotServices = new BotServices();
            this.BotServices.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
                {
                    { "general", LuisTestUtils.GeneralTestUtil.CreateRecognizer() },
                    { "calendar", LuisTestUtils.CalendarTestUtil.CreateRecognizer() },
                    { "email", LuisTestUtils.EmailTestUtil.CreateRecognizer() },
                    { "todo", LuisTestUtils.ToDoTestUtil.CreateRecognizer() },
                    { "pointofinterest", LuisTestUtils.PointOfInterestTestUtil.CreateRecognizer() }
                },
                DispatchRecognizer = DispatchTestUtil.CreateRecognizer()
            });

            // Dummy Authentication connection for Auth testing
            this.BotServices.AuthenticationConnections = new Dictionary<string, string>
            {
                { "DummyAuth", "DummyAuthConnection" }
            };

            // Skill Registration

            // CalendarSkill
            InitialiseSkill(
                "calendarSkill",
                typeof(CalendarSkill.CalendarSkill),
                nameof(Luis.Dispatch.Intent.l_Calendar),
                new string[] { "general", "calendar" },
                new string[] { "DummyAuth" },
                this.BotServices.LocaleConfigurations,
                new string[] { "DummyAuth" });

            // EmailSkill
            InitialiseSkill(
                "emailSkill",
                typeof(EmailSkill.EmailSkill),
                nameof(Luis.Dispatch.Intent.l_Email),
                new string[] { "general", "email" },
                new string[] { "DummyAuth" },
                this.BotServices.LocaleConfigurations,
                new string[] { "DummyAuth" });

            // ToDo
            InitialiseSkill(
                "todoSkill",
                typeof(ToDoSkill.ToDoSkill),
                nameof(Luis.Dispatch.Intent.l_ToDo),
                new string[] { "general", "todo" },
                new string[] { "DummyAuth" },
                this.BotServices.LocaleConfigurations,
                new string[] { "DummyAuth" });

            // ToDo
            InitialiseSkill(
                "pointOfInterestSkill",
                typeof(PointOfInterestSkill.PointOfInterestSkill),
                nameof(Luis.Dispatch.Intent.l_PointOfInterest),
                new string[] { "general", "pointofinterest" },
                new string[] { "DummyAuth" },
                this.BotServices.LocaleConfigurations);
        }

        /// <summary>
        /// Create a TestFlow which spins up a SkillDialog ready for the tests to execute against.
        /// </summary>
        /// <param name="locale">Change the locale of generated activities.</param>
        /// <returns>TestFlow.</returns>
        public TestFlow GetTestFlow(string locale = null)
        {
            var adapter = new TestAdapter(sendTraceActivity: true)
              .Use(new AutoSaveStateMiddleware(this.ConversationState));

            var testFlow = new TestFlow(adapter, async (context, token) =>
            {
                var bot = this.BuildBot() as VirtualAssistant;

                await bot.OnTurnAsync(context, CancellationToken.None);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new VirtualAssistant(this.BotServices, this.ConversationState, this.UserState, this.ProactiveState, this.EndPointService, this.TelemetryClient, this.BackgroundTaskQueue);
        }

        /// <summary>
        /// Wire up each skill into the Virtual Assistant.
        /// </summary>
        /// <param name="skillName">Name of the skill.</param>
        /// <param name="skillType">Assembly reference to skill.</param>
        /// <param name="dispatchIntent">Dispatch Intent.</param>
        /// <param name="luisServiceIds">LUIS service IDs used by skill.</param>
        /// <param name="authConnections">Authentication connections.</param>
        /// <param name="localeConfiguration">Locale configuration.</param>
        /// <param name="supportedProviders">Supported Providers (optional).</param>
        private void InitialiseSkill(string skillName, Type skillType, string dispatchIntent, string[] luisServiceIds, string[] authConnections, Dictionary<string, LocaleConfiguration> localeConfiguration, string[] supportedProviders = null)
        {
            // Prepare skill configuration
            var skillConfiguration = new SkillConfiguration();
            skillConfiguration.AuthenticationConnections.Add("DummyAuth", "DummyAuthConnection");
            skillConfiguration.LocaleConfigurations = this.BotServices.LocaleConfigurations;
            this.BotServices.SkillConfigurations.Add(skillName, skillConfiguration);

            // Skill Registration
            var skillDefinition = new SkillDefinition
            {
                Assembly = skillType.AssemblyQualifiedName,
                Id = skillName,
                Name = skillName,
                DispatchIntent = dispatchIntent,
                LuisServiceIds = luisServiceIds,
                SupportedProviders = supportedProviders,
            };

            this.BotServices.SkillDefinitions.Add(skillDefinition);
        }
    }
}
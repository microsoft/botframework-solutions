using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading.Tasks;
using Autofac;
using FakeSkill.Dialogs.Sample.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Bot.Solutions.Testing.Fakes;
using Microsoft.Bot.Solutions.Tests.Skills.LuisTestUtils;
using Microsoft.Bot.Solutions.Tests.Skills.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Skills
{
    [TestClass]
    public class SkillTestBase : BotTestBase
    {
        public DialogSet Dialogs { get; set; }

        public UserState UserState { get; set; }

        public ConversationState ConversationState { get; set; }

        public IStatePropertyAccessor<DialogState> DialogState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public SkillConfigurationBase Services { get; set; }

        public Dictionary<string, SkillConfigurationBase> SkillConfigurations { get; set; }

        public SkillDialogOptions SkillDialogOptions { get; set; }

        public ConversationReference ConversationReference { get; set; }

        [TestInitialize]
        public new void Initialize()
        {
            var builder = new ContainerBuilder();

            ConversationState = new ConversationState(new MemoryStorage());
            DialogState = ConversationState.CreateProperty<DialogState>(nameof(DialogState));
            UserState = new UserState(new MemoryStorage());
            TelemetryClient = new NullBotTelemetryClient();
            SkillConfigurations = new Dictionary<string, SkillConfigurationBase>();

            // Add the LUIS model fakes used by the Skill
            Services = new MockSkillConfiguration();
            Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
                {
                    { "general", GeneralTestUtil.CreateRecognizer() },
                    { "FakeSkill", FakeSkillTestUtil.CreateRecognizer() }
                }
            });

            Services.LocaleConfigurations.Add("es", new LocaleConfiguration()
            {
                Locale = "es-mx",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
                {
                    { "general", GeneralTestUtil.CreateRecognizer() },
                    { "FakeSkill", FakeSkillTestUtil.CreateRecognizer() }
                }
            });

            // Dummy Authentication connection for Auth testing
            Services.AuthenticationConnections = new Dictionary<string, string>
            {
                { "DummyAuth", "DummyAuthConnection" }
            };

            builder.RegisterInstance(new BotStateSet(UserState, ConversationState));
            Container = builder.Build();

            BotResponseBuilder = new BotResponseBuilder();
            BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());

            Dialogs = new DialogSet(DialogState);

            // Manually mange the conversation metadata when we need finer grained control
            ConversationReference = new ConversationReference
            {
                ChannelId = "test",
                ServiceUrl = "https://test.com",
            };

            ConversationReference.User = new ChannelAccount("user1", "User1");
            ConversationReference.Bot = new ChannelAccount("bot", "Bot");
            ConversationReference.Conversation = new ConversationAccount(false, "convo1", "Conversation1");
        }

        /// <summary>
        /// Create a TestFlow which spins up a CustomSkillDialog ready for the tests to execute against
        /// </summary>
        /// <param name="locale"></param>
        /// <param name="overrideSkillDialogOptions"></param>
        /// <returns></returns>
        public TestFlow GetTestFlow(string locale = null, SkillDialogOptions overrideSkillDialogOptions = null)
        {
            var adapter = new TestAdapter(sendTraceActivity: true)
                .Use(new AutoSaveStateMiddleware(ConversationState));

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
                var dc = await Dialogs.CreateContextAsync(context);

                if (dc.ActiveDialog != null)
                {
                    var result = await dc.ContinueDialogAsync();
                }
                else
                {
                    var options = overrideSkillDialogOptions ?? SkillDialogOptions;
                    await dc.BeginDialogAsync(options.SkillDefinition.Id, options);
                    var result = await dc.ContinueDialogAsync();
                }
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new FakeSkill.FakeSkill(Services, ConversationState, UserState, TelemetryClient, null, true);
        }
    }
}

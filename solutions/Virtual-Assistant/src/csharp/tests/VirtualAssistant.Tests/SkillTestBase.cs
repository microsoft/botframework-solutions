using System;
using System.Collections.Generic;
using Autofac;
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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VirtualAssistant.Tests
{
    /// <summary>
    /// Base class for Skill tests which prepare common configuration such as the LUIS mocks.
    /// </summary>
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

            this.ConversationState = new ConversationState(new MemoryStorage());
            this.DialogState = this.ConversationState.CreateProperty<DialogState>(nameof(this.DialogState));
            this.UserState = new UserState(new MemoryStorage());
            this.TelemetryClient = new NullBotTelemetryClient();
            this.SkillConfigurations = new Dictionary<string, SkillConfigurationBase>();

            // Add the LUIS model fakes used by the Skill
            this.Services = new MockSkillConfiguration();
            this.Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
            {
                { "general", LuisTestUtils.GeneralTestUtil.CreateRecognizer() },
                { "calendar", LuisTestUtils.CalendarTestUtil.CreateRecognizer() },
                { "email", LuisTestUtils.EmailTestUtil.CreateRecognizer() },
                { "todo", LuisTestUtils.ToDoTestUtil.CreateRecognizer() },
                { "pointofinterest", LuisTestUtils.PointOfInterestTestUtil.CreateRecognizer() }
            }
            });

            // Dummy Authentication connection for Auth testing
            this.Services.AuthenticationConnections = new Dictionary<string, string>
            {
                { "DummyAuth", "DummyAuthConnection" }
            };

            builder.RegisterInstance(new BotStateSet(this.UserState, this.ConversationState));
            this.Container = builder.Build();

            this.BotResponseBuilder = new BotResponseBuilder();
            this.BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());

            this.Dialogs = new DialogSet(this.DialogState);

            // Manually mange the conversation metadata when we need finer grained control
            this.ConversationReference = new ConversationReference
            {
                ChannelId = "test",
                ServiceUrl = "https://test.com",
            };

            this.ConversationReference.User = new ChannelAccount("user1", "User1");
            this.ConversationReference.Bot = new ChannelAccount("bot", "Bot");
            this.ConversationReference.Conversation = new ConversationAccount(false, "convo1", "Conversation1");
        }

        /// <summary>
        /// Create a SkillDefinition based on Skill Name and Skill Type.
        /// </summary>
        /// <param name="skillName">Skill Name.</param>
        /// <param name="skillType">Skill Type.</param>
        /// <returns>SkillDefinition.</returns>
        public SkillDefinition CreateSkillDefinition(string skillName, Type skillType)
        {
            var skillDefinition = new SkillDefinition
            {
                Assembly = skillType.AssemblyQualifiedName,
                Id = skillName,
                Name = skillName
            };

            return skillDefinition;
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

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
                var dc = await this.Dialogs.CreateContextAsync(context);

                if (dc.ActiveDialog != null)
                {
                    var result = await dc.ContinueDialogAsync();
                }
                else
                {
                    var options = this.SkillDialogOptions;
                    await dc.BeginDialogAsync(options.SkillDefinition.Id, options);
                    var result = await dc.ContinueDialogAsync();
                }
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return null;
        }
    }
}
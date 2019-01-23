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
        private ConversationReference _conversation { get; set; }

        [TestInitialize]
        public new void Initialize()
        {
            var builder = new ContainerBuilder();

            _conversationState = new ConversationState(new MemoryStorage());
            _dialogState = _conversationState.CreateProperty<DialogState>(nameof(_dialogState));
            _userState = new UserState(new MemoryStorage());
            _telemetryClient = new NullBotTelemetryClient();

            // Add the LUIS model fakes used by the Skill
            _services = new MockSkillConfiguration();
            _services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
                {
                    { "general", GeneralTestUtil.CreateRecognizer() },
                    { "FakeSkill", FakeSkillTestUtil.CreateRecognizer() }
                }
            });
            _services.LocaleConfigurations.Add("es", new LocaleConfiguration()
            {
                Locale = "es-mx",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>
                {
                    { "general", GeneralTestUtil.CreateRecognizer() },
                    { "FakeSkill", FakeSkillTestUtil.CreateRecognizer() }
                }
            });

            // Dummy Authentication connection for Auth testing
            _services.AuthenticationConnections = new Dictionary<string, string>();
            _services.AuthenticationConnections.Add("DummyAuth", "DummyAuthConnection");

            builder.RegisterInstance(new BotStateSet(_userState, _conversationState));
            Container = builder.Build();

            BotResponseBuilder = new BotResponseBuilder();
            BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());

            // Add Fake Skill registration
            const string fakeSkillName = "FakeSkill";
            var fakeSkillDefinition = new SkillDefinition();
            var fakeSkillType = typeof(FakeSkill.FakeSkill);
            fakeSkillDefinition.Assembly = fakeSkillType.AssemblyQualifiedName;
            fakeSkillDefinition.Id = fakeSkillName;
            fakeSkillDefinition.Name = fakeSkillName;

            _skills = new Dictionary<string, SkillConfigurationBase>
            {
                { fakeSkillDefinition.Id, _services }
            };

            // Options are passed to the SkillDialog
            _skillDialogOptions = new SkillDialogOptions();
            _skillDialogOptions.SkillDefinition = fakeSkillDefinition;

            // Add the SkillDialog to the available dialogs passing the initialized FakeSkill
            _dialogs = new DialogSet(_dialogState);
            _dialogs.Add(new CustomSkillDialog(_skills, _dialogState, null, _telemetryClient));

            // Manually mange the conversation metadata when we need finer grained control
            _conversation = new ConversationReference
            {
                ChannelId = "test",
                ServiceUrl = "https://test.com",
            };

            _conversation.User = new ChannelAccount("user1", "User1");
            _conversation.Bot = new ChannelAccount("bot", "Bot");
            _conversation.Conversation = new ConversationAccount(false, "convo1", "Conversation1");
        }

        /// <summary>
        /// Test that we can create a skill and complete a end to end flow includign the EndOfConversation event
        /// signalling the Skill has completed.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task InvokeFakeSkillAndDialog()
        {       
            await GetTestFlow()
               .Send(SampleDialogUtterances.Trigger)
               .AssertReply(MessagePrompt())
               .Send(SampleDialogUtterances.MessagePromptResponse)
               .AssertReply(EchoMessage())
               .AssertReply(this.CheckForEndOfConversationEvent())
               .StartTestAsync();
        }

        /// <summary>
        /// Replica of above test but testing that localisation is working
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task InvokeFakeSkillAndDialog_Spanish()
        {           
            string locale = "es-mx";

            // Set the culture to ES to ensure the reply asserts pull out the spanish variant
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);

            // Use MakeActivity so we can control the locale on the Activity being sent
            await GetTestFlow()
               .Send(MakeActivity(SampleDialogUtterances.Trigger, locale))
               .AssertReply(MessagePrompt())
               .Send(MakeActivity(SampleDialogUtterances.MessagePromptResponse, locale))
               .AssertReply(EchoMessage())
               .AssertReply(this.CheckForEndOfConversationEvent())
               .StartTestAsync();
        }

        /// <summary>
        /// Test the AUth flow behaviour within the SkillDialog
        /// </summary>
        /// <returns></returns>
        //[TestMethod]
        //public async Task InvokeFakeSkillAndDialog_Auth()
        //{
        //    await GetTestFlow()
        //       .Send(SampleDialogUtterances.Auth)
        //       //.AssertReply(MessagePrompt())
        //       //.AssertReply(this.CheckForEndOfConversationEvent())
        //       .StartTestAsync();
        //}

        /// <summary>
        /// Validate that Skill instantiation errors are surfaced as exceptions
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        [ExpectedException(typeof(System.InvalidOperationException),
            "Skill (FakeSkill) could not be created.")]
        public async Task MisconfiguredSkill()
        {
            // Deliberately incorrect assembly reference
            SkillDialogOptions brokenSkillDialogOptions = new SkillDialogOptions();
            brokenSkillDialogOptions.Parameters = _skillDialogOptions.Parameters;
            brokenSkillDialogOptions.SkillDefinition = new SkillDefinition();
            brokenSkillDialogOptions.SkillDefinition.Assembly = "FakeSkill.FakeSkil, Microsoft.Bot.Solutions.Tests, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null";
            brokenSkillDialogOptions.SkillDefinition.Id = "FakeSkill";
            brokenSkillDialogOptions.SkillDefinition.Name = "FakeSkill";

            await GetTestFlow(overrideSkillDialogOptions: brokenSkillDialogOptions)
              .Send(SampleDialogUtterances.Trigger)
              .StartTestAsync();
        }

        /// <summary>
        /// Validate that we have received a EndOfConversation event. The SkillDialog internally consumes this but does
        /// send a Trace Event that we check for the presence of.
        /// </summary>
        /// <returns></returns>
        private Action<IActivity> CheckForEndOfConversationEvent()
        {
            return activity =>
            {
                Activity traceActivity = activity as Activity;
                Assert.IsNotNull(traceActivity);

                Assert.AreEqual(traceActivity.Type, ActivityTypes.Trace);
                Assert.AreEqual(traceActivity.Text, "<--Ending the skill conversation");
            };
        }

        /// <summary>
        ///  Make an activity using the pre-configured Conversation metadata providing a way to control locale
        /// </summary>
        /// <param name="text"></param>
        /// <param name="locale"></param>
        /// <returns></returns>
        public Activity MakeActivity(string text = null, string locale = null)
        {
            Activity activity = new Activity
            {
                Type = ActivityTypes.Message,
                From = _conversation.User,
                Recipient = _conversation.Bot,
                Conversation = _conversation.Conversation,
                ServiceUrl = _conversation.ServiceUrl,
                Text = text,
                Locale = locale ?? null
            };

            return activity;
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
                .Use(new AutoSaveStateMiddleware(_conversationState));

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {
                var dc = await _dialogs.CreateContextAsync(context);

                if (dc.ActiveDialog != null)
                {
                    var result = await dc.ContinueDialogAsync();
                }
                else
                {
                    await dc.BeginDialogAsync(nameof(CustomSkillDialog), overrideSkillDialogOptions ?? _skillDialogOptions);
                    var result = await dc.ContinueDialogAsync();
                }
            });

            return testFlow;
        }

        private Action<IActivity> MessagePrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(ParseReplies(SampleResponses.MessagePrompt.Replies, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> EchoMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(ParseReplies(SampleResponses.MessageResponse.Replies, new[] { SampleDialogUtterances.MessagePromptResponse }), messageActivity.Text);
            };
        }

        public override IBot BuildBot()
        {
            return new FakeSkill.FakeSkill(_services, _conversationState, _userState, _telemetryClient, null, true);
        }
    }
}

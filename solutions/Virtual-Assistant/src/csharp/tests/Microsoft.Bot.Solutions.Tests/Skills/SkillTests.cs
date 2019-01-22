using Autofac;
using FakeSkill.Dialogs.Sample.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Bot.Solutions.Testing.Fakes;
using Microsoft.Bot.Solutions.Tests.Skills.LuisTestUtils;
using Microsoft.Bot.Solutions.Tests.Skills.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Tests.Skills
{
   [TestClass]
   public class SkillTests : BotTestBase
    {
        public ConversationState ConversationState { get; set; }
        public IStatePropertyAccessor<DialogState> DialogState { get; set; }
        public DialogSet Dialogs { get; set; }

        public UserState UserState { get; set; }

        public IBotTelemetryClient TelemetryClient { get; set; }

        public SkillConfigurationBase Services { get; set; }
        public SkillDialogOptions SkillDialogOptions { get; set; }
        public Dictionary<string, SkillConfigurationBase> Skills;

        public ConversationReference Conversation { get; set; }

        [TestInitialize]
        public new void Initialize()
        {
            var builder = new ContainerBuilder();

            ConversationState = new ConversationState(new MemoryStorage());
            DialogState = ConversationState.CreateProperty<DialogState>(nameof(DialogState));
            UserState = new UserState(new MemoryStorage());
            TelemetryClient = new NullBotTelemetryClient();         

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

            builder.RegisterInstance(new BotStateSet(UserState, ConversationState));
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

            Skills = new Dictionary<string, SkillConfigurationBase>
            {
                { fakeSkillDefinition.Id, Services }
            };

            // Options are passed to the SkillDialog
            SkillDialogOptions = new SkillDialogOptions();
            SkillDialogOptions.SkillDefinition = fakeSkillDefinition;

            // Add the SkillDialog to the available dialogs passing the initialized FakeSkill
            Dialogs = new DialogSet(DialogState);
            Dialogs.Add(new CustomSkillDialog(Skills, DialogState, null, TelemetryClient));            

            Conversation = new ConversationReference
                {
                    ChannelId = "test",
                    ServiceUrl = "https://test.com",
                };

            Conversation.User = new ChannelAccount("user1", "User1");
            Conversation.Bot = new ChannelAccount("bot", "Bot");
            Conversation.Conversation = new ConversationAccount(false, "convo1", "Conversation1");
        }

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

        [TestMethod]
        [ExpectedException(typeof(System.InvalidOperationException), "Skill (FakeSkill) could not be created.")]
        public async Task MisconfiguredSkill()
        {
            // Deliberately incorrect assembly reference
            SkillDialogOptions brokenSkillDialogOptions = new SkillDialogOptions();
            brokenSkillDialogOptions.Parameters = SkillDialogOptions.Parameters;
            brokenSkillDialogOptions.SkillDefinition = new SkillDefinition();
            brokenSkillDialogOptions.SkillDefinition.Assembly = "FakeSkill.FakeSkil, Microsoft.Bot.Solutions.Tests, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null";
            brokenSkillDialogOptions.SkillDefinition.Id = "FakeSkill";
            brokenSkillDialogOptions.SkillDefinition.Name = "FakeSkill";

            await GetTestFlow(overrideSkillDialogOptions: brokenSkillDialogOptions)
              .Send(SampleDialogUtterances.Trigger)
              .StartTestAsync();
        }

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

        public Activity MakeActivity(string text = null, string locale = null)
        {
            Activity activity = new Activity
            {
                Type = ActivityTypes.Message,
                From = Conversation.User,
                Recipient = Conversation.Bot,
                Conversation = Conversation.Conversation,
                ServiceUrl = Conversation.ServiceUrl,
                Text = text,
                Locale = locale ?? null
            };

            return activity;
        }

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
                    await dc.BeginDialogAsync(nameof(CustomSkillDialog), overrideSkillDialogOptions ?? SkillDialogOptions);
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
            return new FakeSkill.FakeSkill(Services, ConversationState, UserState, TelemetryClient, null, true);
        }
    }
}

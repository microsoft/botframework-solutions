using Autofac;
using FakeSkill.Dialogs.Sample.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
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
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        public SkillDialogOptions skillDialogOptions { get; set; }
        public Dictionary<string, SkillConfigurationBase> Skills;

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

            builder.RegisterInstance(new BotStateSet(UserState, ConversationState));
            Container = builder.Build();

            BotResponseBuilder = new BotResponseBuilder();
            BotResponseBuilder.AddFormatter(new TextBotResponseFormatter());
            
            // Add Fake Skill registration
            var fakeSkillDefinition = new SkillDefinition();
            var fakeSkillType = typeof(FakeSkill.FakeSkill);
            fakeSkillDefinition.Assembly = fakeSkillType.AssemblyQualifiedName;
            fakeSkillDefinition.Id = "FakeSkill";
            fakeSkillDefinition.Name = "FakeSkill";

            Skills = new Dictionary<string, SkillConfigurationBase>();
            Skills.Add(fakeSkillDefinition.Id,Services);

            // Options are passed to the SkillDialog
            skillDialogOptions = new SkillDialogOptions();
            skillDialogOptions.SkillDefinition = fakeSkillDefinition;

            // Add the SkillDialog to the available dialogs passing the initialized FakeSkill
            Dialogs = new DialogSet(DialogState);
            Dialogs.Add(new SkillDialog(Skills, DialogState, null, TelemetryClient, false));
        }

        [TestMethod]
        public async Task InvokeMockSkill()
        {
            await GetTestFlow()
               .Send(SampleDialogUtterances.Trigger)
               .AssertReply(MessagePrompt())
               .Send(SampleDialogUtterances.MessagePromptResponse)
               .AssertReply(EchoMessage())
               .AssertReply(ActionEndMessage())
               .StartTestAsync();
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

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }

        public TestFlow GetTestFlow()
        {
            var adapter = new TestAdapter()
              .Use(new AutoSaveStateMiddleware(ConversationState));

            var testFlow = new TestFlow(adapter, async (context, cancellationToken) =>
            {              
                // Spin up the Skill Dialog that the Test will send messages to.
                var dc = await Dialogs.CreateContextAsync(context, cancellationToken);
                var result = await dc.BeginDialogAsync(nameof(SkillDialog), skillDialogOptions);
                await dc.ContinueDialogAsync(cancellationToken);
            });

            return testFlow;
        }

        public override IBot BuildBot()
        {
            return new FakeSkill.FakeSkill(Services, ConversationState, UserState, TelemetryClient, null, true);
        }
    }
}

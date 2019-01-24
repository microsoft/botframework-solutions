using FakeSkill.Dialogs.Sample.Resources;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Tests.Skills.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Tests.Skills
{
    [TestClass]
    public class InvokeSkillTests : SkillTestBase
    {
        [TestInitialize]
        public void InitSkills()
        {
            // Add Fake Skill registration
            const string fakeSkillName = "FakeSkill";
            var fakeSkillDefinition = new SkillDefinition();
            var fakeSkillType = typeof(FakeSkill.FakeSkill);
            fakeSkillDefinition.Assembly = fakeSkillType.AssemblyQualifiedName;
            fakeSkillDefinition.Id = fakeSkillName;
            fakeSkillDefinition.Name = fakeSkillName;

            SkillConfigurations.Add(fakeSkillDefinition.Id, Services);

            // Options are passed to the SkillDialog
            SkillDialogOptions = new SkillDialogOptions();
            SkillDialogOptions.SkillDefinition = fakeSkillDefinition;

            // Add the SkillDialog to the available dialogs passing the initialized FakeSkill
            Dialogs.Add(new SkillDialog(fakeSkillDefinition, Services, null, TelemetryClient));
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
            var locale = "es-mx";

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
        ///  Make an activity using the pre-configured Conversation metadata providing a way to control locale
        /// </summary>
        /// <param name="text"></param>
        /// <param name="locale"></param>
        /// <returns></returns>
        public Activity MakeActivity(string text = null, string locale = null)
        {
            var activity = new Activity
            {
                Type = ActivityTypes.Message,
                From = ConversationReference.User,
                Recipient = ConversationReference.Bot,
                Conversation = ConversationReference.Conversation,
                ServiceUrl = ConversationReference.ServiceUrl,
                Text = text,
                Locale = locale ?? null
            };

            return activity;
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
                var traceActivity = activity as Activity;
                Assert.IsNotNull(traceActivity);

                Assert.AreEqual(traceActivity.Type, ActivityTypes.Trace);
                Assert.AreEqual(traceActivity.Text, "<--Ending the skill conversation");
            };
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
    }
}

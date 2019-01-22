using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using $safeprojectname$.Flow.Utterances;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using $ext_safeprojectname$.Dialogs.Main.Resources;
using $ext_safeprojectname$.Dialogs.Sample.Resources;

namespace $safeprojectname$.Flow
{
    [TestClass]
    public class InterruptionTests : $ext_safeprojectname$TestBase
{
        [TestMethod]
        public async Task Test_Help_Interruption()
        {
            await GetTestFlow()
               .Send(SampleDialogUtterances.Trigger)
               .AssertReply(MessagePrompt())
               .Send(GeneralUtterances.Help)
               .AssertReply(HelpResponse())
               .AssertReply(MessagePrompt())
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Cancel_Interruption()
        {
            await GetTestFlow()
               .Send(SampleDialogUtterances.Trigger)
               .AssertReply(MessagePrompt())
               .Send(GeneralUtterances.Cancel)
               .AssertReply(CancelResponse())
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

        private Action<IActivity> HelpResponse()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(ParseReplies(MainResponses.HelpMessage.Replies, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> CancelResponse()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(ParseReplies(MainResponses.CancelMessage.Replies, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }
    }
}

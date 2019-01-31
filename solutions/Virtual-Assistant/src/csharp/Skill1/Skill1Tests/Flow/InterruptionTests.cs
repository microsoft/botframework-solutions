using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skill1Tests.Flow.Utterances;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Skill1.Dialogs.Main.Resources;
using Skill1.Dialogs.Sample.Resources;

namespace Skill1Tests.Flow
{
    [TestClass]
    public class InterruptionTests : Skill1TestBase
    {
        [TestMethod]
        public async Task Test_Help_Interruption()
        {
            await GetTestFlow()
               .Send(SampleDialogUtterances.Trigger)
               .AssertReply(NamePrompt())
               .Send(GeneralUtterances.Help)
               .AssertReply(HelpResponse())
               .AssertReply(NamePrompt())
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Cancel_Interruption()
        {
            await GetTestFlow()
               .Send(SampleDialogUtterances.Trigger)
               .AssertReply(NamePrompt())
               .Send(GeneralUtterances.Cancel)
               .AssertReply(CancelResponse())
               .StartTestAsync();
        }

        private Action<IActivity> NamePrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(ParseReplies(SampleResponses.NamePrompt.Replies, new StringDictionary()), messageActivity.Text);
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
    }
}

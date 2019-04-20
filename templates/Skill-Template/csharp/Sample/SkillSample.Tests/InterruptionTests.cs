using SkillSample.Tests.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System;
using SkillSample.Responses.Sample;
using System.Collections.Specialized;
using SkillSample.Responses.Main;

namespace SkillSample.Tests
{
    [TestClass]
    public class InterruptionTests : SkillTestBase
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
                CollectionAssert.Contains(ParseReplies(SampleResponses.NamePrompt, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> HelpResponse()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(ParseReplies(MainResponses.HelpMessage, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> CancelResponse()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(ParseReplies(MainResponses.CancelMessage, new StringDictionary()), messageActivity.Text);
            };
        }
    }
}

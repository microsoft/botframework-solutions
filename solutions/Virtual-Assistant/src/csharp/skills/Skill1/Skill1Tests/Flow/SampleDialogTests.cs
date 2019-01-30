using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skill1Tests.Flow.Utterances;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Skill1.Dialogs.Sample.Resources;

namespace Skill1Tests.Flow
{
    [TestClass]
    public class SampleDialogTests : Skill1TestBase
    {
        [TestMethod]
        public async Task Test_Sample_Dialog()
        {
            await GetTestFlow()
               .Send(SampleDialogUtterances.Trigger)
               .AssertReply(NamePrompt())
               .Send(SampleDialogUtterances.MessagePromptResponse)
               .AssertReply(HaveNameMessage())
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

        private Action<IActivity> HaveNameMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(ParseReplies(SampleResponses.HaveNameMessage.Replies, new StringDictionary() { { "Name", SampleDialogUtterances.MessagePromptResponse } }), messageActivity.Text);
            };
        }

    }
}

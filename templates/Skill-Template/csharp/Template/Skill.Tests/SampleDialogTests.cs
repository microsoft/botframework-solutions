using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using $ext_safeprojectname$.Responses.Sample;
using $safeprojectname$.Utterances;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace $safeprojectname$
{
    [TestClass]
    public class SampleDialogTests : SkillTestBase
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
                CollectionAssert.Contains(ParseReplies(SampleResponses.NamePrompt, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> HaveNameMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(ParseReplies(SampleResponses.HaveNameMessage, new StringDictionary() { { "Name", SampleDialogUtterances.MessagePromptResponse } }), messageActivity.Text);
            };
        }

    }
}

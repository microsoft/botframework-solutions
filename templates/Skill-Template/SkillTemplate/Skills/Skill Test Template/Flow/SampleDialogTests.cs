using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using $safeprojectname$.Flow.Utterances;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using $ext_safeprojectname$.Dialogs.Sample.Resources;

namespace $safeprojectname$.Flow
{
    [TestClass]
    public class SampleDialogTests : $ext_safeprojectname$TestBase
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

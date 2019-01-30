using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using PointOfInterestSkillTests.Flow.Utterances;

namespace PointOfInterestSkillTests.Flow
{
    [TestClass]
    public class PointOfInterestDialogTests : PointOfInterestTestBase
    {
        [TestMethod]
        public async Task Test_Sample_Dialog()
        {
            await GetTestFlow()
               .Send(PointOfInterestDialogUtterances.LocationEvent)
               .AssertReply(MessagePrompt())
               //.Send(PointOfInterestDialogUtterances.MessagePromptResponse)
               //.AssertReply(EchoMessage())
               .AssertReply(ActionEndMessage())
               .StartTestAsync();
        }

        private Action<IActivity> MessagePrompt()
        {
            //return activity =>
            //{
            //    var messageActivity = activity.AsMessageActivity();
            //    CollectionAssert.Contains(ParseReplies(SampleResponses.MessagePrompt.Replies, new StringDictionary()), messageActivity.Text);
            //};

            throw new NotImplementedException();
        }

        private Action<IActivity> EchoMessage()
        {
            //return activity =>
            //{
            //    var messageActivity = activity.AsMessageActivity();
            //    CollectionAssert.Contains(ParseReplies(SampleResponses.MessageResponse.Replies, new[] { SampleDialogUtterances.MessagePromptResponse }), messageActivity.Text);
            //};

            throw new NotImplementedException();
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

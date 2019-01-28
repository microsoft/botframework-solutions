using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestSkillTests.Flow.Utterances;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using TestSkill.Dialogs.Sample.Resources;
using Microsoft.Bot.Solutions.Resources;
using TestSkill.Dialogs.Main.Resources;

namespace TestSkillTests.Flow
{
    [TestClass]
    public class SampleDialogTests : TestSkillTestBase
    {
        private JsonTemplateManager _responder = new JsonTemplateManager(new IResponseIdCollection[]
        {
            new SampleResponses(),
            new MainResponses()
        });

        [TestMethod]
        public async Task Test_Sample_Dialog()
        {
            await GetTestFlow()
               .Send(SampleDialogUtterances.Trigger)
               .AssertReply(NamePrompt())
               .Send(SampleDialogUtterances.MessagePromptResponse)
               .AssertReply(EchoMessage())
               .AssertReply(ActionEndMessage())
               .StartTestAsync();
        }

        private Action<IActivity> NamePrompt()
        {
            return activity =>
            {
                var messageActivity = activity as Activity;
                var botResponse = _responder.GetBotResponse(SampleResponses.NamePrompt, messageActivity.Locale);
                CollectionAssert.Contains(ParseReplies(botResponse.Replies, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> EchoMessage()
        {
            return activity =>
            {
                var messageActivity = activity as Activity;
                var botResponse = _responder.GetBotResponse(SampleResponses.HaveNameMessage, messageActivity.Locale);
                CollectionAssert.Contains(ParseReplies(botResponse.Replies, new[] { SampleDialogUtterances.MessagePromptResponse }), messageActivity.Text);
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

using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using TestSkill.Dialogs.Main.Resources;
using TestSkill.Dialogs.Shared.Resources;

namespace TestSkillTests.Flow
{
    [TestClass]
    public class MainDialogTests : TestSkillTestBase
    {
        private JsonTemplateManager _responder = new JsonTemplateManager(new [] { typeof(MainResponses), typeof(SharedResponses)});

        [TestMethod]
        public async Task Test_Unhandled_Message()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReply(DidntUnderstandMessage())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> DidntUnderstandMessage()
        {
            return activity =>
            {
                var messageActivity = activity as Activity;
                var botResponse = _responder.GetBotResponse(SharedResponses.DidntUnderstandMessage, messageActivity.Locale);
                CollectionAssert.Contains(ParseReplies(botResponse.Replies, new StringDictionary()), messageActivity.Text);
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

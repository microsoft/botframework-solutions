using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using Skill1.Dialogs.Main.Resources;
using Skill1.Dialogs.Shared.Resources;
using Skill1Tests.Flow.Utterances;

namespace Skill1Tests.Flow
{
    [TestClass]
    public class MainDialogTests : Skill1TestBase
    {
        [TestMethod]
        public async Task Test_Intro_Message()
        {
            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("bot") }
                })
                .AssertReply(IntroMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Help_Intent()
        {
            await GetTestFlow()
                .Send(GeneralUtterances.Help)
                .AssertReply(HelpMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Unhandled_Message()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReply(DidntUnderstandMessage())
                .StartTestAsync();
        }

        private Action<IActivity> IntroMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(ParseReplies(MainResponses.WelcomeMessage.Replies, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> HelpMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(ParseReplies(MainResponses.HelpMessage.Replies, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> DidntUnderstandMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(ParseReplies(SharedResponses.DidntUnderstandMessage.Replies, new StringDictionary()), messageActivity.Text);
            };
        }
    }
}

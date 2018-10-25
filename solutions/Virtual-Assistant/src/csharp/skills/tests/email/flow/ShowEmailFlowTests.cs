using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.ShowEmail.Resources;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.Flow
{
    [TestClass]
    public class ShowEmailFlowTests : EmailBotTestBase
    {
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
        }

        [TestMethod]
        public async Task Test_NotSendingEmail()
        {
            await this.GetTestFlow()
                .Send("Show Emails")
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send("The first one")
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send("No")
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] AfterSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.SentSuccessfully.Replies, new StringDictionary());
        }

        private string[] ActionEndMessage()
        {
            return this.ParseReplies(EmailSharedResponses.ActionEnded.Replies, new StringDictionary());
        }

        private string[] ReadOutPrompt()
        {
            return this.ParseReplies(ShowEmailResponses.ReadOutPrompt.Replies, new StringDictionary());
        }

        private string[] ReadOutMorePrompt()
        {
            return this.ParseReplies(ShowEmailResponses.ReadOutMorePrompt.Replies, new StringDictionary());
        }

        private Action<IActivity> AssertSelectOneOfTheMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(ShowEmailResponses.ReadOutMessage.Replies, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private Action<IActivity> AssertComfirmBeforeSendingPrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ConfirmSend.Replies, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private Action<IActivity> ShowEmailList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ShowEmailPrompt.Replies, new StringDictionary() { { "SearchType", "relevant unread" } }), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 5);
            };
        }

        private Action<IActivity> ShowNextPage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ShowEmailPrompt.Replies, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }
    }
}

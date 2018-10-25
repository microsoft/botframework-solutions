using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.Flow
{
    [TestClass]
    public class ForwardEmailFlowTests : EmailBotTestBase
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
                .Send("Forward Email")
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send("TestName")
                .AssertReply(this.ShowEmailList())
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .Send("The first one")
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send("TestContent")
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send("No")
                .AssertReplyOneOf(this.NotSendingMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendingEmail()
        {
            await this.GetTestFlow()
                .Send("Forward Email")
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send("TestName")
                .AssertReply(this.ShowEmailList())
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .Send("The first one")
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send("TestContent")
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send("Yes")
                .AssertReplyOneOf(this.AfterSendingMessage())
                .StartTestAsync();
        }

        private string[] AfterSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.SentSuccessfully.Replies, new StringDictionary());
        }

        private string[] NotSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.CancellingMessage.Replies, new StringDictionary());
        }

        private Action<IActivity> ShowEmailList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var replies = this.ParseReplies(EmailSharedResponses.ShowEmailPrompt.Replies, new StringDictionary() { { "SearchType", "relevant unread" } });
                CollectionAssert.Contains(replies, messageActivity.Text);
                Assert.AreNotEqual(messageActivity.Attachments.Count, 0);
            };
        }

        private Action<IActivity> AssertSelectOneOfTheMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.NoFocusMessage.Replies, new StringDictionary()), messageActivity.Text);
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

        private string[] CollectRecipientsMessage()
        {
            return this.ParseReplies(EmailSharedResponses.NoRecipients.Replies, new StringDictionary());
        }

        private string[] CollectFocusedMessage()
        {
            return this.ParseReplies(EmailSharedResponses.NoFocusMessage.Replies, new StringDictionary());
        }

        private string[] CollectEmailContentMessage()
        {
            return this.ParseReplies(EmailSharedResponses.NoEmailContent.Replies, new StringDictionary());
        }
    }
}

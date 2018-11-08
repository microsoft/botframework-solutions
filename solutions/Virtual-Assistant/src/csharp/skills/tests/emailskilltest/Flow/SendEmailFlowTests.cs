using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Dialogs.ConfirmRecipient.Resources;
using EmailSkill.Dialogs.SendEmail.Resources;
using EmailSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.Flow
{
    [TestClass]
    public class SendEmailFlowTests : EmailBotTestBase
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
                .Send("Send Email")
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send("TestName")
                .AssertReply(this.CollectSubjcetMessage())
                .Send("TestSubjcet")
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send("TestContent")
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send("No")
                .AssertReplyOneOf(this.NotSendingMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_NotSendingEmailWithMultiUserSelect_Ordinal()
        {
            await this.GetTestFlow()
                .Send("Send Email")
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send("TestDupName")
                .AssertReply(this.CollectRecipients())
                .Send("The first one")
                .AssertReply(this.CollectSubjcetMessageDup())
                .Send("TestSubjcet")
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send("TestContent")
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send("No")
                .AssertReplyOneOf(this.NotSendingMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_NotSendingEmailWithMultiUserSelect_Number()
        {
            await this.GetTestFlow()
                .Send("Send Email")
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send("TestDupName")
                .AssertReply(this.CollectRecipients())
                .Send("1")
                .AssertReply(this.CollectSubjcetMessageDup())
                .Send("TestSubjcet")
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send("TestContent")
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send("No")
                .AssertReplyOneOf(this.NotSendingMessage())
                .StartTestAsync();
        }

        private string[] NotSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.ActionEnded.Replies, new StringDictionary());
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

        private Action<IActivity> CollectRecipients()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(ConfirmRecipientResponses.ConfirmRecipientLastPage.Replies, new StringDictionary());

                Assert.IsTrue(recipientConfirmedMessage.Length == 1);
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
            };
        }

        private Action<IActivity> CollectSubjcetMessageDup()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(EmailSharedResponses.RecipientConfirmed.Replies, new StringDictionary() { { "UserName", "TestDup Test" } });
                var noSubjectMessage = this.ParseReplies(SendEmailResponses.NoSubject.Replies, new StringDictionary());

                string[] subjectVerifyInfo = new string[recipientConfirmedMessage.Length * noSubjectMessage.Length];
                int index = -1;
                foreach (var confirmNsg in recipientConfirmedMessage)
                {
                    foreach (var noSubjectMsg in noSubjectMessage)
                    {
                        index++;
                        subjectVerifyInfo[index] = confirmNsg + " " + noSubjectMsg;
                    }
                }

                CollectionAssert.Contains(subjectVerifyInfo, messageActivity.Text);
            };
        }

        private Action<IActivity> CollectSubjcetMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(EmailSharedResponses.RecipientConfirmed.Replies, new StringDictionary() { { "UserName", "Test Test" } });
                var noSubjectMessage = this.ParseReplies(SendEmailResponses.NoSubject.Replies, new StringDictionary());

                string[] subjectVerifyInfo = new string[recipientConfirmedMessage.Length * noSubjectMessage.Length];
                int index = -1;
                foreach (var confirmNsg in recipientConfirmedMessage)
                {
                    foreach (var noSubjectMsg in noSubjectMessage)
                    {
                        index++;
                        subjectVerifyInfo[index] = confirmNsg + " " + noSubjectMsg;
                    }
                }

                CollectionAssert.Contains(subjectVerifyInfo, messageActivity.Text);
            };
        }

        private string[] CollectEmailContentMessage()
        {
            return this.ParseReplies(SendEmailResponses.NoMessageBody.Replies, new StringDictionary());
        }
    }
}

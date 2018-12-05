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
        [TestMethod]
        public async Task Test_NotSendingEmail()
        {
            string testRecipient = "Test Test";
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send("Send Email")
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send("TestSubjcet")
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send("TestContent")
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send("No")
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_NotSendingEmailWithMultiUserSelect_Ordinal()
        {
            string testRecipient = "TestDup Test";
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send("Send Email")
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReply(this.CollectRecipients())
                .Send("The first one")
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send("TestSubjcet")
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send("TestContent")
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send("No")
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_NotSendingEmailWithMultiUserSelect_Number()
        {
            string testRecipient = "TestDup Test";
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send("Send Email")
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReply(this.CollectRecipients())
                .Send("1")
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send("TestSubjcet")
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send("TestContent")
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send("No")
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_NotSendingEmailWithEmailAdressInput()
        {
            string testRecipient = "test@test.com";
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send("Send email to " + testRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send("TestSubjcet")
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send("TestContent")
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send("No")
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_NotSendingEmailWithEmailAdressConfirm()
        {
            string testRecipient = "Nobody";
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };
            string testRecipientConfirm = "test@test.com";
            StringDictionary recipientConfirmDict = new StringDictionary() { { "UserName", testRecipientConfirm } };

            await this.GetTestFlow()
                .Send("Send email to " + testRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.RecipientNotFoundMessage(recipientDict))
                .Send(testRecipientConfirm)
                .AssertReply(this.CollectSubjectMessage(recipientConfirmDict))
                .Send("TestSubjcet")
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send("TestContent")
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send("No")
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] NotSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.ActionEnded.Replies, new StringDictionary());
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
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

        private Action<IActivity> CollectSubjectMessage(StringDictionary recipients)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(EmailSharedResponses.RecipientConfirmed.Replies, recipients);
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

        private Action<IActivity> RecipientNotFoundMessage(StringDictionary recipients)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientNotFoundMessage = this.ParseReplies(ConfirmRecipientResponses.PromptPersonNotFound.Replies, recipients);

                CollectionAssert.Contains(recipientNotFoundMessage, messageActivity.Text);
            };
        }

        private string[] CollectEmailContentMessage()
        {
            return this.ParseReplies(SendEmailResponses.NoMessageBody.Replies, new StringDictionary());
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var eventActivity = activity.AsEventActivity();
                Assert.AreEqual(eventActivity.Name, "tokens/request");
            };
        }
    }
}

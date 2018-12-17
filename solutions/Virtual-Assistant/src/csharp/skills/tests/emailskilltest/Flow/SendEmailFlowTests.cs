using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Dialogs.ConfirmRecipient.Resources;
using EmailSkill.Dialogs.SendEmail.Resources;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkillTest.Flow.Fakes;
using EmailSkillTest.Flow.Strings;
using EmailSkillTest.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.Flow
{
    [TestClass]
    public class SendEmailFlowTests : EmailBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var luisServices = this.Services.LocaleConfigurations["en"].LuisServices;
            luisServices.Clear();
            luisServices.Add("email", new MockEmailLuisRecognizer(new SendEmailUtterances()));
            luisServices.Add("general", new MockGeneralLuisRecognizer());
        }

        [TestMethod]
        public async Task Test_NotSendingEmail()
        {
            string testRecipient = ContextStrings.TestRecipient;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendingEmail()
        {
            string testRecipient = ContextStrings.TestRecipient;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AfterSendingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToRecipient()
        {
            string testRecipient = ContextStrings.TestRecipient;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AfterSendingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToRecipientWithSubject()
        {
            string testRecipient = ContextStrings.TestRecipient;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToRecipientWithSubject)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CollectContextMessageWithUserInfo(recipientDict))
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AfterSendingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToRecipientWithSubjectAndContext()
        {
            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToRecipientWithSubjectAndContext)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AfterSendingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithMultiUserSelect_Ordinal()
        {
            string testRecipient = ContextStrings.TestRecipientWithDup;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReply(this.CollectRecipients())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithMultiUserSelect_Number()
        {
            string testRecipient = ContextStrings.TestRecipientWithDup;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReply(this.CollectRecipients())
                .Send(BaseTestUtterances.NumberOne)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithEmailAdressInput()
        {
            string testRecipient = ContextStrings.TestEmailAdress;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToEmailAdress)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithEmailAdressConfirm()
        {
            string testRecipient = ContextStrings.Nobody;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };
            string testRecipientConfirm = ContextStrings.TestEmailAdress;
            StringDictionary recipientConfirmDict = new StringDictionary() { { "UserName", testRecipientConfirm } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToNobody)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.RecipientNotFoundMessage(recipientDict))
                .Send(testRecipientConfirm)
                .AssertReply(this.CollectSubjectMessage(recipientConfirmDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToMultiRecipient()
        {
            string testRecipient = ContextStrings.TestRecipient + " and " + ContextStrings.TestRecipientWithDup;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToMultiRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CollectRecipients())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToEmpty()
        {
            string testRecipient = ContextStrings.TestRecipient;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(ContextStrings.TestEmptyRecipient)
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(ContextStrings.TestRecipient)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] AfterSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.SentSuccessfully.Replies, new StringDictionary());
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

        private Action<IActivity> CollectContextMessageWithUserInfo(StringDictionary recipients)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(EmailSharedResponses.RecipientConfirmed.Replies, recipients);
                var noMessage = this.ParseReplies(SendEmailResponses.NoMessageBody.Replies, new StringDictionary());

                string[] verifyInfo = new string[recipientConfirmedMessage.Length * noMessage.Length];
                int index = -1;
                foreach (var confirmNsg in recipientConfirmedMessage)
                {
                    foreach (var noSubjectMsg in noMessage)
                    {
                        index++;
                        verifyInfo[index] = confirmNsg + " " + noSubjectMsg;
                    }
                }

                CollectionAssert.Contains(verifyInfo, messageActivity.Text);
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

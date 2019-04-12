using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Responses.FindContact;
using EmailSkill.Responses.Shared;
using EmailSkill.Utilities;
using EmailSkillTest.Flow.Fakes;
using EmailSkillTest.Flow.Strings;
using EmailSkillTest.Flow.Utterances;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.Flow
{
    [TestClass]
    public class ForwardEmailFlowTests : EmailBotTestBase
    {
        [TestMethod]
        public async Task Test_NotSendingEmail()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await this.GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(ContextStrings.TestRecipient)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendingEmail()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await this.GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(ContextStrings.TestRecipient)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(string.Format(EmailCommonStrings.ForwardReplyFormat, ContextStrings.TestSubject + "0")))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ForwardEmailToRecipient()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await this.GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmailsToRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(string.Format(EmailCommonStrings.ForwardReplyFormat, ContextStrings.TestSubject + "0")))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ForwardEmailToRecipientWithContent()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await this.GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmailsToRecipientWithContent)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(string.Format(EmailCommonStrings.ForwardReplyFormat, ContextStrings.TestSubject + "0")))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ForwardEmailWhenNoEmailIsShown()
        {
            // Setup email data
            var serviceManager = this.ServiceManager as MockServiceManager;
            serviceManager.MailService.MyMessages = serviceManager.MailService.FakeMyMessages(0);

            await this.GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.EmailNotFoundPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }

        private string[] ConfirmOneNameOneAddress(StringDictionary recipientDict)
        {
            return this.ParseReplies(FindContactResponses.PromptOneNameOneAddress, recipientDict);
        }

        private string[] EmailNotFoundPrompt()
        {
            return this.ParseReplies(EmailSharedResponses.EmailNotFound, new StringDictionary());
        }

        private Action<IActivity> AfterSendingMessage(string subject)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                var stringToken = new StringDictionary
                {
                    { "Subject", subject },
                };

                var replies = this.ParseReplies(EmailSharedResponses.SentSuccessfully, stringToken);
                CollectionAssert.Contains(replies, messageActivity.Text);
            };
        }

        private string[] NotSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.CancellingMessage, new StringDictionary());
        }

        private Action<IActivity> ShowEmailList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                // Get showed mails:
                var showedItems = ((MockServiceManager)this.ServiceManager).MailService.MyMessages;
                var replies = this.ParseReplies(EmailSharedResponses.ShowEmailPrompt, new StringDictionary()
                {
                    { "TotalCount", showedItems.Count.ToString() },
                    { "EmailListDetails", SpeakHelper.ToSpeechEmailListString(showedItems, TimeZoneInfo.Local, ConfigData.GetInstance().MaxReadSize) },
                });

                CollectionAssert.Contains(replies, messageActivity.Text);
                Assert.AreNotEqual(messageActivity.Attachments.Count, 0);
            };
        }

        private Action<IActivity> AssertSelectOneOfTheMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.NoFocusMessage, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> AssertComfirmBeforeSendingPrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ConfirmSend, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private string[] CollectRecipientsMessage()
        {
            return this.ParseReplies(EmailSharedResponses.NoRecipients, new StringDictionary());
        }

        private string[] CollectFocusedMessage()
        {
            return this.ParseReplies(EmailSharedResponses.NoFocusMessage, new StringDictionary());
        }

        private string[] CollectEmailContentMessage()
        {
            return this.ParseReplies(EmailSharedResponses.NoEmailContent, new StringDictionary());
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var message = activity.AsMessageActivity();
                Assert.AreEqual(1, message.Attachments.Count);
                Assert.AreEqual("application/vnd.microsoft.card.oauth", message.Attachments[0].ContentType);
            };
        }
    }
}

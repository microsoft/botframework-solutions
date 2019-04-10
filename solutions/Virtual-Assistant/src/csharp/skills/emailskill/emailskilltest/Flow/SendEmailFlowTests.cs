using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Responses.FindContact;
using EmailSkill.Responses.SendEmail;
using EmailSkill.Responses.Shared;
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
        [TestMethod]
        public async Task Test_NotSendingEmail()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback())
                .AssertReply(this.AssertCheckContent())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendingEmail()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback())
                .AssertReply(this.AssertCheckContent())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToRecipient()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback())
                .AssertReply(this.AssertCheckContent())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToRecipientWithSubject()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToRecipientWithSubject)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToRecipientWithSubjectAndContext()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToRecipientWithSubjectAndContext)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithMultiUserSelect_Ordinal()
        {
            var testRecipient = ContextStrings.TestRecipientWithDup;
            var testEmail = ContextStrings.TestDupEmail;
            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmail } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReply(this.ConfirmEmail(recipientDict))
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback())
                .AssertReply(this.AssertCheckContent())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithMultiUserSelect_Number()
        {
            var testRecipient = ContextStrings.TestRecipientWithDup;
            var testEmail = ContextStrings.TestDupEmail;
            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmail } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReply(this.ConfirmEmail(recipientDict))
                .Send(BaseTestUtterances.NumberOne)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback())
                .AssertReply(this.AssertCheckContent())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithEmailAdressInput()
        {
            var testRecipient = ContextStrings.TestEmailAdress;
            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToEmailAdress)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback())
                .AssertReply(this.AssertCheckContent())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithEmailAdressConfirm()
        {
            var testRecipient = ContextStrings.Nobody;
            var recipientDict = new StringDictionary() { { "UserName", testRecipient } };
            var testRecipientConfirm = ContextStrings.TestEmailAdress;
            var recipientConfirmDict = new StringDictionary() { { "UserName", testRecipientConfirm }, { "EmailAddress", testRecipientConfirm } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToNobody)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CoundNotFindUser(recipientDict))
                .Send(testRecipientConfirm)
                .AssertReply(this.CollectSubjectMessage(recipientConfirmDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback())
                .AssertReply(this.AssertCheckContent())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToMultiRecipient()
        {
            var testDupRecipient = ContextStrings.TestRecipientWithDup;
            var testDupEmail = ContextStrings.TestDupEmail;
            var recipientDupDict = new StringDictionary() { { "UserName", testDupRecipient }, { "EmailAddress", testDupEmail } };

            var testRecipient = ContextStrings.TestRecipient;
            var testEmail = ContextStrings.TestEmailAdress;
            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmail } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToMultiRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.BeforeConfirmMultiName(recipientDict, recipientDupDict))
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.ConfirmEmailDetails(recipientDupDict))
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.CollectMultiUserSubjectMessage(recipientDict, recipientDupDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback())
                .AssertReply(this.AssertCheckContent())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToEmpty()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(ContextStrings.TestEmptyRecipient)
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(ContextStrings.TestRecipient)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback())
                .AssertReply(this.AssertCheckContent())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
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

        private string[] ConfirmOneNameOneAddress(StringDictionary recipientDict)
        {
            return this.ParseReplies(FindContactResponses.PromptOneNameOneAddress, recipientDict);
        }

        private string[] AfterSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.SentSuccessfully, new StringDictionary());
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }

        private Action<IActivity> AssertContentPlayback()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(SendEmailResponses.PlayBackMessage, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private Action<IActivity> AssertCheckContent()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(SendEmailResponses.CheckContent, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> CoundNotFindUser(StringDictionary recipient)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(FindContactResponses.UserNotFound, recipient), messageActivity.Text);
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

        private Action<IActivity> CollectRecipients()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(FindContactResponses.ConfirmMultiplContactEmailMultiPage, new StringDictionary());

                Assert.IsTrue(recipientConfirmedMessage.Length == 1);
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
            };
        }

        private Action<IActivity> BeforeConfirmMultiName(StringDictionary recipientDict, StringDictionary recipientDupDict)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(FindContactResponses.BeforeSendingMessage, new StringDictionary() { { "NameList", recipientDict["UserName"] + " and " + recipientDupDict["UserName"] } });

                Assert.IsTrue(recipientConfirmedMessage.Length == 1);
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
            };
        }

        private Action<IActivity> BeforeConfirmName(StringDictionary recipientDict)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(FindContactResponses.BeforeSendingMessage, new StringDictionary() { { "NameList", recipientDict["UserName"] } });

                Assert.IsTrue(recipientConfirmedMessage.Length == 1);
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
            };
        }

        private Action<IActivity> ConfirmtName(StringDictionary recipientDict)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(FindContactResponses.ConfirmMultipleContactNameMultiPage, recipientDict);

                Assert.IsTrue(recipientConfirmedMessage.Length == 1);
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
            };
        }

        private Action<IActivity> ConfirmEmailDetails(StringDictionary recipientDict)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(FindContactResponses.ConfirmMultiplContactEmailMultiPage, recipientDict);

                Assert.IsTrue(recipientConfirmedMessage.Length == 1);
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
            };
        }

        private Action<IActivity> ConfirmEmail(StringDictionary recipientDict)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(FindContactResponses.ConfirmMultiplContactEmailMultiPage, recipientDict);

                Assert.IsTrue(recipientConfirmedMessage.Length == 1);
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
            };
        }

        private Action<IActivity> CollectMultiUserSubjectMessage(StringDictionary recipients, StringDictionary multiRecipients)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                recipients["UserName"] += ": " + recipients["EmailAddress"] + " and " + multiRecipients["UserName"] + ": " + multiRecipients["EmailAddress"];
                var recipientConfirmedMessage = this.ParseReplies(EmailSharedResponses.RecipientConfirmed, recipients);
                var noSubjectMessage = this.ParseReplies(SendEmailResponses.NoSubject, new StringDictionary());

                var subjectVerifyInfo = new string[recipientConfirmedMessage.Length * noSubjectMessage.Length];
                var index = -1;
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

        private Action<IActivity> CollectSubjectMessage(StringDictionary recipients)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                recipients["UserName"] += ": " + recipients["EmailAddress"];
                var recipientConfirmedMessage = this.ParseReplies(EmailSharedResponses.RecipientConfirmed, recipients);
                var noSubjectMessage = this.ParseReplies(SendEmailResponses.NoSubject, new StringDictionary());

                var subjectVerifyInfo = new string[recipientConfirmedMessage.Length * noSubjectMessage.Length];
                var index = -1;
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
                recipients["UserName"] += ": " + recipients["EmailAddress"];
                var recipientConfirmedMessage = this.ParseReplies(EmailSharedResponses.RecipientConfirmed, recipients);
                var noMessage = this.ParseReplies(SendEmailResponses.NoMessageBody, new StringDictionary());

                var verifyInfo = new string[recipientConfirmedMessage.Length * noMessage.Length];
                var index = -1;
                foreach (var confirmNsg in recipientConfirmedMessage)
                {
                    foreach (var noSubjectMsg in noMessage)
                    {
                        index++;
                        verifyInfo[index] = confirmNsg + " " + noSubjectMsg;
                    }
                }

                CollectionAssert.Contains(noMessage, messageActivity.Text);
            };
        }

        private string[] CollectEmailContentMessage()
        {
            return this.ParseReplies(SendEmailResponses.NoMessageBody, new StringDictionary());
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

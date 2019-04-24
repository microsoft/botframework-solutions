using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Dialogs.FindContact.Resources;
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
            string testEmailAddress = ContextStrings.TestEmailAdress;

            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };
            StringDictionary recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmailAddress } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
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
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;

            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };
            StringDictionary recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmailAddress } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
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
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;

            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };
            StringDictionary recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmailAddress } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
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
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;

            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };
            StringDictionary recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmailAddress } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToRecipientWithSubject)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToRecipientWithSubjectAndContext()
        {
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;

            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };
            StringDictionary recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmailAddress } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToRecipientWithSubjectAndContext)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithMultiUserSelect_Ordinal()
        {
            string testRecipient = ContextStrings.TestRecipientWithDup;
            string testEmail = ContextStrings.TestDupEmail;

            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmail } };
            StringDictionary recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmail } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReply(this.ConfirmEmail(recipientDict))
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
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
            string testRecipient = ContextStrings.TestRecipientWithDup;
            string testEmail = ContextStrings.TestDupEmail;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmail } };
            StringDictionary recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmail } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(testRecipient)
                .AssertReply(this.ConfirmEmail(recipientDict))
                .Send(BaseTestUtterances.NumberOne)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
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
            string testRecipient = ContextStrings.TestEmailAdress;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testRecipient } };
            StringDictionary recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToEmailAdress)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
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
            string testRecipient = ContextStrings.Nobody;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };
            string testRecipientConfirm = ContextStrings.TestEmailAdress;
            StringDictionary recipientConfirmDict = new StringDictionary() { { "UserName", testRecipientConfirm }, { "EmailAddress", testRecipientConfirm } };
            StringDictionary recipientList = new StringDictionary() { { "NameList", testRecipientConfirm + ": " + testRecipientConfirm } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToNobody)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CoundNotFindUser(recipientDict))
                .Send(testRecipientConfirm)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
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
            string testDupRecipient = ContextStrings.TestRecipientWithDup;
            string testDupEmail = ContextStrings.TestDupEmail;
            StringDictionary recipientDupDict = new StringDictionary() { { "UserName", testDupRecipient }, { "EmailAddress", testDupEmail } };

            string testRecipient = ContextStrings.TestRecipient;
            string testEmail = ContextStrings.TestEmailAdress;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmail } };

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
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;

            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };
            StringDictionary recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmailAddress } };

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
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
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

        private string[] AddMoreContacts(StringDictionary recipientDict)
        {
            return this.ParseReplies(FindContactResponses.AddMoreContactsPrompt, recipientDict);
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
                var confirmSend = this.ParseReplies(EmailSharedResponses.ConfirmSend, new StringDictionary());
                Assert.IsTrue(messageActivity.Text.StartsWith(confirmSend[0]));
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

        private Action<IActivity> CollectSubjectMessage(StringDictionary recipients)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                recipients["UserName"] += ": " + recipients["EmailAddress"];
                var recipientConfirmedMessage = this.ParseReplies(EmailSharedResponses.RecipientConfirmed, recipients);
                var noSubjectMessage = this.ParseReplies(SendEmailResponses.NoSubject, new StringDictionary());

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
                recipients["UserName"] += ": " + recipients["EmailAddress"];
                var recipientConfirmedMessage = this.ParseReplies(EmailSharedResponses.RecipientConfirmed, recipients);
                var noMessage = this.ParseReplies(SendEmailResponses.NoMessageBody, new StringDictionary());

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
                var eventActivity = activity.AsEventActivity();
                Assert.AreEqual(eventActivity.Name, "tokens/request");
            };
        }
    }
}

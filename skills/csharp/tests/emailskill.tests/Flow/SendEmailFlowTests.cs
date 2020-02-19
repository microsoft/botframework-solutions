// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Responses.FindContact;
using EmailSkill.Responses.Main;
using EmailSkill.Responses.SendEmail;
using EmailSkill.Responses.Shared;
using EmailSkill.Tests.Flow.Strings;
using EmailSkill.Tests.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class SendEmailFlowTests : EmailSkillTestBase
    {
        [TestMethod]
        public async Task Test_NotSendingEmail()
        {
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new
            {
                UserName = testRecipient,
                EmailAddress = testEmailAddress
            };

            var recipientWithAddressDict = new
            {
                UserName = testRecipient + ": " + testEmailAddress
            };

            var recipientList = new
            {
                NameList = testRecipient + ": " + testEmailAddress
            };

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(SendEmailUtterances.SendEmails)
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
                .AssertReply(this.AssertContentPlayback(ContextStrings.TestContent))
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

            var recipientDict = new
            {
                UserName = testRecipient,
                EmailAddress = testEmailAddress
            };

            var recipientWithAddressDict = new
            {
                UserName = testRecipient + ": " + testEmailAddress
            };

            var recipientList = new
            {
                NameList = testRecipient + ": " + testEmailAddress
            };

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(SendEmailUtterances.SendEmails)
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
                .AssertReply(this.AssertContentPlayback(ContextStrings.TestContent))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToRecipient()
        {
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new
            {
                UserName = testRecipient,
                EmailAddress = testEmailAddress
            };

            var recipientWithAddressDict = new
            {
                UserName = testRecipient + ": " + testEmailAddress
            };

            var recipientList = new
            {
                NameList = testRecipient + ": " + testEmailAddress
            };

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(SendEmailUtterances.SendEmailToRecipient)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback(ContextStrings.TestContent))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToRecipientWithSubjectAndContext()
        {
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new
            {
                UserName = testRecipient,
                EmailAddress = testEmailAddress
            };

            var recipientList = new
            {
                NameList = testRecipient + ": " + testEmailAddress
            };

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(SendEmailUtterances.SendEmailToRecipientWithSubjectAndContext)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithMultiUserSelect_Ordinal()
        {
            string testRecipient = ContextStrings.TestRecipientWithDup;
            string testEmail = ContextStrings.TestDupEmail;

            var recipientDict = new
            {
                UserName = testRecipient,
                EmailAddress = testEmail
            };

            var recipientWithAddressDict = new
            {
                UserName = testRecipient + ": " + testEmail
            };

            var recipientList = new
            {
                NameList = testRecipient + ": " + testEmail
            };

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(SendEmailUtterances.SendEmails)
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
                .AssertReply(this.AssertContentPlayback(ContextStrings.TestContent))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithMultiUserSelect_Number()
        {
            string testRecipient = ContextStrings.TestRecipientWithDup;
            string testEmail = ContextStrings.TestDupEmail;

            var recipientDict = new
            {
                UserName = testRecipient,
                EmailAddress = testEmail
            };

            var recipientWithAddressDict = new
            {
                UserName = testRecipient + ": " + testEmail
            };

            var recipientList = new
            {
                NameList = testRecipient + ": " + testEmail
            };

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(SendEmailUtterances.SendEmails)
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
                .AssertReply(this.AssertContentPlayback(ContextStrings.TestContent))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithEmailAdressInput()
        {
            string testRecipient = ContextStrings.TestEmailAdress;

            var recipientDict = new
            {
                UserName = testRecipient
            };

            var recipientList = new
            {
                NameList = testRecipient
            };

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(SendEmailUtterances.SendEmailToEmailAdress)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback(ContextStrings.TestContent))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailWithEmailAdressConfirm()
        {
            string testRecipient = ContextStrings.Nobody;
            string testRecipientConfirm = ContextStrings.TestEmailAdress;

            var recipientDict = new
            {
                UserName = testRecipient,
            };

            var recipientConfirmDict = new
            {
                UserName = testRecipientConfirm
            };

            var recipientList = new
            {
                NameList = testRecipientConfirm
            };

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(SendEmailUtterances.SendEmailToNobody)
                .AssertReply(this.CoundNotFindUser(recipientDict))
                .Send(testRecipientConfirm)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.CollectSubjectMessage(recipientConfirmDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback(ContextStrings.TestContent))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToMultiRecipient()
        {
            string testRecipient = ContextStrings.TestRecipient;
            string testEmail = ContextStrings.TestEmailAdress;

            string testDupRecipient = ContextStrings.TestRecipientWithDup;
            string testDupEmail = ContextStrings.TestDupEmail;

            var recipientDict = new
            {
                UserName = testRecipient,
                EmailAddress = testEmail
            };

            var recipientDupDict = new
            {
                UserName = testDupRecipient,
                EmailAddress = testDupEmail
            };

            var multiNameDict = new
            {
                NameList = testRecipient + " and " + testDupRecipient
            };

            var multiNameWithAddressDict = new
            {
                UserName = testRecipient + " and " + testDupRecipient
            };

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(SendEmailUtterances.SendEmailToMultiRecipient)
                .AssertReply(this.BeforeConfirmMultiName(multiNameDict))
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.ConfirmEmailDetails(recipientDupDict))
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.CollectMultiUserSubjectMessage(multiNameWithAddressDict))
                .Send(ContextStrings.TestSubject)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertContentPlayback(ContextStrings.TestContent))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendEmailToEmpty()
        {
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new
            {
                UserName = testRecipient,
                EmailAddress = testEmailAddress
            };

            var recipientList = new
            {
                NameList = testRecipient + ": " + testEmailAddress
            };

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(SendEmailUtterances.SendEmails)
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
                .AssertReply(this.AssertContentPlayback(ContextStrings.TestContent))
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AfterSendingMessage(ContextStrings.TestSubject))
                .StartTestAsync();
        }

        private Action<IActivity> AfterSendingMessage(string subject)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                var replies = GetTemplates(EmailSharedResponses.SentSuccessfully, new { Subject = subject });
                CollectionAssert.Contains(replies, messageActivity.Text);
            };
        }

        private string[] ConfirmOneNameOneAddress(object recipientDict)
        {
            return GetTemplates(FindContactResponses.PromptOneNameOneAddress, recipientDict);
        }

        private string[] AddMoreContacts(object recipientDict)
        {
            return GetTemplates(FindContactResponses.AddMoreContactsPrompt, recipientDict);
        }

        private Action<IActivity> AssertContentPlayback(string content)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(GetTemplates(SendEmailResponses.PlayBackMessage, new { emailcontent = content }), messageActivity.Text);
            };
        }

        private Action<IActivity> AssertCheckContent()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(GetTemplates(SendEmailResponses.CheckContent), messageActivity.Text);
            };
        }

        private Action<IActivity> CoundNotFindUser(object recipient)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(GetTemplates(FindContactResponses.UserNotFound, recipient), messageActivity.Text);
            };
        }

        private Action<IActivity> AssertComfirmBeforeSendingPrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var confirmSend = GetTemplates(EmailSharedResponses.ConfirmSend);
                Assert.IsTrue(messageActivity.Text.StartsWith(confirmSend[0]));
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private string[] CollectRecipientsMessage()
        {
            return GetTemplates(EmailSharedResponses.NoRecipients);
        }

        private Action<IActivity> CollectRecipients()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = GetTemplates(FindContactResponses.ConfirmMultiplContactEmailMultiPage);

                Assert.IsTrue(recipientConfirmedMessage.Length == 1);
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
            };
        }

        private Action<IActivity> BeforeConfirmMultiName(object multiNameDict)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = GetTemplates(FindContactResponses.BeforeSendingMessage, multiNameDict);

                Assert.IsTrue(recipientConfirmedMessage.Length == 1);
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
            };
        }

        private Action<IActivity> ConfirmEmailDetails(object recipientDict)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = GetTemplates(FindContactResponses.ConfirmMultiplContactEmailMultiPage, recipientDict);

                Assert.IsTrue(recipientConfirmedMessage.Length == 1);
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
            };
        }

        private Action<IActivity> ConfirmEmail(object recipientDict)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = GetTemplates(FindContactResponses.ConfirmMultiplContactEmailMultiPage, recipientDict);

                Assert.IsTrue(recipientConfirmedMessage.Length == 1);
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
            };
        }

        private Action<IActivity> CollectMultiUserSubjectMessage(object recipients)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                var recipientConfirmedMessage = GetTemplates(EmailSharedResponses.RecipientConfirmed, recipients);
                var noSubjectMessage = GetTemplates(SendEmailResponses.NoSubject);

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

        private Action<IActivity> CollectSubjectMessage(object recipientList)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                var recipientConfirmedMessage = GetTemplates(EmailSharedResponses.RecipientConfirmed, recipientList);
                var noSubjectMessage = GetTemplates(SendEmailResponses.NoSubject);

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
            return GetTemplates(SendEmailResponses.NoMessageBody);
        }
    }
}

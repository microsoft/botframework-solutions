﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Responses.FindContact;
using EmailSkill.Responses.Shared;
using EmailSkill.Tests.Flow.Fakes;
using EmailSkill.Tests.Flow.Strings;
using EmailSkill.Tests.Flow.Utterances;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkill.Tests.Flow
{
    [TestClass]
    public class ForwardEmailFlowTests : EmailSkillTestBase
    {
        [TestMethod]
        public async Task Test_NotSendingEmail()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new
            {
                UserName = testRecipient,
                EmailAddress = testEmailAddress
            };

            var recipientList = new
            {
                NameList = testRecipient + ": " + testEmailAddress
            };

            await GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmails)
                .AssertReply(ShowEmailList())
                .AssertReply(AssertSelectOneOfTheMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(CollectRecipientsMessage())
                .Send(ContextStrings.TestRecipient)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
                .AssertReply(CollectEmailContentMessageForForward(testRecipient))
                .Send(ContextStrings.TestContent)
                .AssertReply(AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(NotSendingMessage())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendingEmail()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new
            {
                UserName = testRecipient,
                EmailAddress = testEmailAddress
            };

            var recipientList = new
            {
                NameList = testRecipient + ": " + testEmailAddress
            };

            await GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmails)
                .AssertReply(ShowEmailList())
                .AssertReply(AssertSelectOneOfTheMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(CollectRecipientsMessage())
                .Send(ContextStrings.TestRecipient)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
                .AssertReply(CollectEmailContentMessageForForward(testRecipient))
                .Send(ContextStrings.TestContent)
                .AssertReply(AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(AfterSendingMessage(string.Format(EmailCommonStrings.ForwardReplyFormat, ContextStrings.TestSubject + "0")))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ForwardEmailToRecipient()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new
            {
                UserName = testRecipient,
                EmailAddress = testEmailAddress
            };

            var recipientList = new
            {
                NameList = testRecipient + ": " + testEmailAddress
            };

            await GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmailsToRecipient)
                .AssertReply(ShowEmailList())
                .AssertReply(AssertSelectOneOfTheMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
                .AssertReply(CollectEmailContentMessageForForward(testRecipient))
                .Send(ContextStrings.TestContent)
                .AssertReply(AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(AfterSendingMessage(string.Format(EmailCommonStrings.ForwardReplyFormat, ContextStrings.TestSubject + "0")))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ForwardEmailToRecipientWithContent()
        {
            var testRecipient = ContextStrings.TestRecipient;
            var testEmailAddress = ContextStrings.TestEmailAdress;

            var recipientDict = new
            {
                UserName = testRecipient,
                EmailAddress = testEmailAddress
            };

            var recipientList = new
            {
                NameList = testRecipient + ": " + testEmailAddress
            };

            await GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmailsToRecipientWithContent)
                .AssertReply(ShowEmailList())
                .AssertReply(AssertSelectOneOfTheMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
                .AssertReply(AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(AfterSendingMessage(string.Format(EmailCommonStrings.ForwardReplyFormat, ContextStrings.TestSubject + "0")))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ForwardEmailWhenNoEmailIsShown()
        {
            // Setup email data
            var serviceManager = ServiceManager as MockServiceManager;
            serviceManager.MailService.MyMessages = serviceManager.MailService.FakeMyMessages(0);

            await GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmails)
                .AssertReplyOneOf(EmailNotFoundPrompt())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Handoff);
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

        private string[] EmailNotFoundPrompt()
        {
            return GetTemplates(EmailSharedResponses.EmailNotFound);
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

        private string[] NotSendingMessage()
        {
            return GetTemplates(EmailSharedResponses.CancellingMessage);
        }

        private Action<IActivity> ShowEmailList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                // Get showed mails:
                var showedItems = ServiceManager.MailService.MyMessages;

                var replies = GetTemplates(
                    EmailSharedResponses.ShowEmailPrompt,
                    new
                    {
                        TotalCount = showedItems.Count.ToString(),
                        EmailListDetails = SpeakHelper.ToSpeechEmailListString(showedItems, TimeZoneInfo.Local, ConfigData.GetInstance().MaxReadSize)
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

                CollectionAssert.Contains(GetTemplates(EmailSharedResponses.NoFocusMessage), messageActivity.Text);
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

        private Action<IActivity> CollectEmailContentMessageForForward(string userName)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                var noEmailContentMessages = GetTemplates(EmailSharedResponses.NoEmailContentForForward);
                var recipientConfirmedMessages = GetTemplates(EmailSharedResponses.RecipientConfirmed, new { userName = userName });

                var allReply = new List<string>();
                foreach (var recipientConfirmedMessage in recipientConfirmedMessages)
                {
                    foreach (var noEmailContentMessage in noEmailContentMessages)
                    {
                        allReply.Add(recipientConfirmedMessage + " " + noEmailContentMessage);
                    }
                }

                CollectionAssert.Contains(allReply, messageActivity.Text);
            };
        }
    }
}

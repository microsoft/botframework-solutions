﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };
            var recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmailAddress } };

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

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };
            var recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmailAddress } };

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

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };
            var recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmailAddress } };

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

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };
            var recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmailAddress } };

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

        private string[] ConfirmOneNameOneAddress(StringDictionary recipientDict)
        {
            return ParseReplies(FindContactResponses.PromptOneNameOneAddress, recipientDict);
        }

        private string[] AddMoreContacts(StringDictionary recipientDict)
        {
            return ParseReplies(FindContactResponses.AddMoreContactsPrompt, recipientDict);
        }

        private string[] EmailNotFoundPrompt()
        {
            return ParseReplies(EmailSharedResponses.EmailNotFound, new StringDictionary());
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

                var replies = ParseReplies(EmailSharedResponses.SentSuccessfully, stringToken);
                CollectionAssert.Contains(replies, messageActivity.Text);
            };
        }

        private string[] NotSendingMessage()
        {
            return ParseReplies(EmailSharedResponses.CancellingMessage, new StringDictionary());
        }

        private Action<IActivity> ShowEmailList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                // Get showed mails:
                var showedItems = ServiceManager.MailService.MyMessages;
                var replies = ParseReplies(EmailSharedResponses.ShowEmailPrompt, new StringDictionary()
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

                CollectionAssert.Contains(ParseReplies(EmailSharedResponses.NoFocusMessage, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> AssertComfirmBeforeSendingPrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var confirmSend = ParseReplies(EmailSharedResponses.ConfirmSend, new StringDictionary());
                Assert.IsTrue(messageActivity.Text.StartsWith(confirmSend[0]));
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private string[] CollectRecipientsMessage()
        {
            return ParseReplies(EmailSharedResponses.NoRecipients, new StringDictionary());
        }

        private string[] CollectFocusedMessage()
        {
            return ParseReplies(EmailSharedResponses.NoFocusMessage, new StringDictionary());
        }

        private Action<IActivity> CollectEmailContentMessageForForward(string userName)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                var noEmailContentMessage = ResponseManager.GetResponse(EmailSharedResponses.NoEmailContentForForward);
                var recipientConfirmedMessage = ResponseManager.GetResponse(EmailSharedResponses.RecipientConfirmed, new StringDictionary() { { "UserName", userName } });
                noEmailContentMessage.Text = recipientConfirmedMessage.Text + " " + noEmailContentMessage.Text;
                noEmailContentMessage.Speak = recipientConfirmedMessage.Speak + " " + noEmailContentMessage.Speak;

                Assert.AreEqual(noEmailContentMessage.Text, messageActivity.Text);
            };
        }
    }
}

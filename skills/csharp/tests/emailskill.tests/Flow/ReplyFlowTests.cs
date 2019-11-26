// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Responses.Shared;
using EmailSkill.Tests.Flow.Strings;
using EmailSkill.Tests.Flow.Utterances;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkill.Tests.Flow
{
    [TestClass]
    public class ReplyFlowTests : EmailSkillTestBase
    {
        [TestMethod]
        public async Task Test_NotSendingEmail()
        {
            await GetTestFlow()
                .Send(ReplyEmailUtterances.ReplyEmails)
                .AssertReply(ShowEmailList())
                .AssertReplyOneOf(NoFocusMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(CollectEmailContentMessageForReply())
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
            await GetTestFlow()
                .Send(ReplyEmailUtterances.ReplyEmails)
                .AssertReply(ShowEmailList())
                .AssertReplyOneOf(NoFocusMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(CollectEmailContentMessageForReply())
                .Send(ContextStrings.TestContent)
                .AssertReply(AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(AfterSendingMessage(string.Format(EmailCommonStrings.ReplyReplyFormat, ContextStrings.TestSubject + "0")))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ReplyEmailWithContent()
        {
            await GetTestFlow()
                .Send(ReplyEmailUtterances.ReplyEmailsWithContent)
                .AssertReply(ShowEmailList())
                .AssertReplyOneOf(NoFocusMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(AfterSendingMessage(string.Format(EmailCommonStrings.ReplyReplyFormat, ContextStrings.TestSubject + "0")))
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

        private string[] NotSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.CancellingMessage, new StringDictionary());
        }

        private string[] NoFocusMessage()
        {
            return this.ParseReplies(EmailSharedResponses.NoFocusMessage, new StringDictionary());
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

        private Action<IActivity> ShowEmailList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                // Get showed mails:
                var showedItems = ServiceManager.MailService.MyMessages;
                var replies = this.ParseReplies(EmailSharedResponses.ShowEmailPrompt, new StringDictionary()
                {
                    { "TotalCount", showedItems.Count.ToString() },
                    { "EmailListDetails", SpeakHelper.ToSpeechEmailListString(showedItems, TimeZoneInfo.Local, ConfigData.GetInstance().MaxReadSize) },
                });

                CollectionAssert.Contains(replies, messageActivity.Text);
                Assert.AreNotEqual(messageActivity.Attachments.Count, 0);
            };
        }

        private string[] CollectEmailContentMessageForReply()
        {
            return this.ParseReplies(EmailSharedResponses.NoEmailContentForReply, new StringDictionary());
        }
    }
}

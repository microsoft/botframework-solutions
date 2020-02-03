// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Responses.Main;
using EmailSkill.Responses.Shared;
using EmailSkill.Tests.Flow.Strings;
using EmailSkill.Tests.Flow.Utterances;
using EmailSkill.Utilities;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class ReplyFlowTests : EmailSkillTestBase
    {
        [TestMethod]
        public async Task Test_NotSendingEmail()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.EmailWelcomeMessage))
                .Send(ReplyEmailUtterances.ReplyEmails)
                .AssertReply(ShowEmailList())
                .AssertReplyOneOf(NoFocusMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(CollectEmailContentMessageForReply())
                .Send(ContextStrings.TestContent)
                .AssertReply(AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(NotSendingMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendingEmail()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.EmailWelcomeMessage))
                .Send(ReplyEmailUtterances.ReplyEmails)
                .AssertReply(ShowEmailList())
                .AssertReplyOneOf(NoFocusMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(CollectEmailContentMessageForReply())
                .Send(ContextStrings.TestContent)
                .AssertReply(AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(AfterSendingMessage(string.Format(EmailCommonStrings.ReplyReplyFormat, ContextStrings.TestSubject + "0")))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ReplyEmailWithContent()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.EmailWelcomeMessage))
                .Send(ReplyEmailUtterances.ReplyEmailsWithContent)
                .AssertReply(ShowEmailList())
                .AssertReplyOneOf(NoFocusMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(AfterSendingMessage(string.Format(EmailCommonStrings.ReplyReplyFormat, ContextStrings.TestSubject + "0")))
                .StartTestAsync();
        }

        private string[] NotSendingMessage()
        {
            return GetTemplates(EmailSharedResponses.CancellingMessage);
        }

        private string[] NoFocusMessage()
        {
            return GetTemplates(EmailSharedResponses.NoFocusMessage);
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

                var replies = GetTemplates(EmailSharedResponses.SentSuccessfully, new { Subject = subject });
                CollectionAssert.Contains(replies, messageActivity.Text);
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

        private Action<IActivity> ShowEmailList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                // Get showed mails:
                var showedItems = ServiceManager.MailService.MyMessages;

                var replies = GetTemplates(EmailSharedResponses.ShowEmailPrompt, new
                {
                    TotalCount = showedItems.Count.ToString(),
                    EmailListDetails = SpeakHelper.ToSpeechEmailListString(showedItems, TimeZoneInfo.Local, ConfigData.GetInstance().MaxReadSize)
                });

                CollectionAssert.Contains(replies, messageActivity.Text);
                Assert.AreNotEqual(messageActivity.Attachments.Count, 0);
            };
        }

        private string[] CollectEmailContentMessageForReply()
        {
            return GetTemplates(EmailSharedResponses.NoEmailContentForReply);
        }
    }
}

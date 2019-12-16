﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Responses.DeleteEmail;
using EmailSkill.Responses.Shared;
using EmailSkill.Tests.Flow.Fakes;
using EmailSkill.Tests.Flow.Utterances;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkill.Tests.Flow
{
    [TestClass]
    public class DeleteEmailFlowTests : EmailSkillTestBase
    {
        [TestMethod]
        public async Task Test_NotDeleteEmail()
        {
            await this.GetTestFlow()
                .Send(DeleteEmailUtterances.DeleteEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.NoFocusMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.DeleteConfirm())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteEmail()
        {
            await this.GetTestFlow()
                .Send(DeleteEmailUtterances.DeleteEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.NoFocusMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.DeleteConfirm())
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.DeleteSuccess())
                .AssertReply(this.ActionEndMessage())
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

        private string[] DeleteSuccess()
        {
            return GetTemplates(DeleteEmailResponses.DeleteSuccessfully);
        }

        private Action<IActivity> DeleteConfirm()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(GetTemplates(DeleteEmailResponses.DeleteConfirm), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Handoff);
            };
        }

        private Action<IActivity> AssertComfirmBeforeSendingPrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(GetTemplates(EmailSharedResponses.ConfirmSend), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private Action<IActivity> ShowEmailList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                // Get showed mails:
                var showedItems = ((MockServiceManager)this.ServiceManager).MailService.MyMessages;

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
    }
}

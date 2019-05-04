using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Utilities;
using EmailSkillTest.Flow.Fakes;
using EmailSkillTest.Flow.Strings;
using EmailSkillTest.Flow.Utterances;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.Flow
{
    [TestClass]
    public class ReplyFlowTests : EmailBotTestBase
    {
        [TestMethod]
        public async Task Test_NotSendingEmail()
        {
            await GetTestFlow()
                .Send(ReplyEmailUtterances.ReplyEmails)
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReply(ShowEmailList())
                .AssertReplyOneOf(NoFocusMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(CollectEmailContentMessage())
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
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReply(ShowEmailList())
                .AssertReplyOneOf(NoFocusMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(CollectEmailContentMessage())
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
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
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
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
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

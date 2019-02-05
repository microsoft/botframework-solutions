using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Util;
using EmailSkillTest.Flow.Fakes;
using EmailSkillTest.Flow.Strings;
using EmailSkillTest.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.Flow
{
    [TestClass]
    public class ForwardEmailFlowTests : EmailBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var luisServices = this.Services.LocaleConfigurations["en"].LuisServices;
            luisServices.Clear();
            luisServices.Add("email", new MockEmailLuisRecognizer(new ForwardEmailUtterances()));
            luisServices.Add("general", new MockGeneralLuisRecognizer());
        }

        [TestMethod]
        public async Task Test_NotSendingEmail()
        {
            await this.GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(ContextStrings.TestRecipient)
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
            await this.GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(ContextStrings.TestRecipient)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AfterSendingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ForwardEmailToRecipient()
        {
            await this.GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmailsToRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AfterSendingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ForwardEmailToRecipientWithContent()
        {
            await this.GetTestFlow()
                .Send(ForwardEmailUtterances.ForwardEmailsToRecipientWithContent)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AfterSendingMessage())
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

        private string[] EmailNotFoundPrompt()
        {
            var response = ResponseManager.GetResponseTemplate(EmailSharedResponses.EmailNotFound);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] AfterSendingMessage()
        {
            var response = ResponseManager.GetResponseTemplate(EmailSharedResponses.SentSuccessfully);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] NotSendingMessage()
        {
            var response = ResponseManager.GetResponseTemplate(EmailSharedResponses.CancellingMessage);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private Action<IActivity> ShowEmailList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                // Get showed mails:
                var showedItems = ((MockServiceManager)this.ServiceManager).MailService.MyMessages;
                var response = ResponseManager.GetResponseTemplate(EmailSharedResponses.ShowEmailPrompt);
                var replies = this.ParseReplies(response.Replies, new StringDictionary()
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
                var response = ResponseManager.GetResponseTemplate(EmailSharedResponses.NoFocusMessage);
                CollectionAssert.Contains(this.ParseReplies(response.Replies, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> AssertComfirmBeforeSendingPrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var response = ResponseManager.GetResponseTemplate(EmailSharedResponses.ConfirmSend);
                CollectionAssert.Contains(this.ParseReplies(response.Replies, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private string[] CollectRecipientsMessage()
        {
            var response = ResponseManager.GetResponseTemplate(EmailSharedResponses.NoRecipients);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] CollectFocusedMessage()
        {
            var response = ResponseManager.GetResponseTemplate(EmailSharedResponses.NoFocusMessage);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] CollectEmailContentMessage()
        {
            var response = ResponseManager.GetResponseTemplate(EmailSharedResponses.NoEmailContent);
            return this.ParseReplies(response.Replies, new StringDictionary());
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

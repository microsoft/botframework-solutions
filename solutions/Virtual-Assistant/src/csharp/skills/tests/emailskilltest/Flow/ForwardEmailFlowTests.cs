using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkillTest.Flow.Fakes;
using EmailSkillTest.Flow.Strings;
using EmailSkillTest.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.Flow
{
    [TestClass]
    public class ForwardEmailFlowTests : EmailBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var luisServices = this.Services.LuisServices;
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
                .StartTestAsync();
        }

        private string[] AfterSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.SentSuccessfully.Replies, new StringDictionary());
        }

        private string[] NotSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.CancellingMessage.Replies, new StringDictionary());
        }

        private Action<IActivity> ShowEmailList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var replies = this.ParseReplies(EmailSharedResponses.ShowEmailPrompt.Replies, new StringDictionary() { { "SearchType", "relevant unread" } });
                CollectionAssert.Contains(replies, messageActivity.Text);
                Assert.AreNotEqual(messageActivity.Attachments.Count, 0);
            };
        }

        private Action<IActivity> AssertSelectOneOfTheMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.NoFocusMessage.Replies, new StringDictionary()), messageActivity.Text);
            };
        }

        private Action<IActivity> AssertComfirmBeforeSendingPrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ConfirmSend.Replies, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private string[] CollectRecipientsMessage()
        {
            return this.ParseReplies(EmailSharedResponses.NoRecipients.Replies, new StringDictionary());
        }

        private string[] CollectFocusedMessage()
        {
            return this.ParseReplies(EmailSharedResponses.NoFocusMessage.Replies, new StringDictionary());
        }

        private string[] CollectEmailContentMessage()
        {
            return this.ParseReplies(EmailSharedResponses.NoEmailContent.Replies, new StringDictionary());
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

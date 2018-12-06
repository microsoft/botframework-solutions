using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.ShowEmail.Resources;
using EmailSkillTest.Flow.Fakes;
using EmailSkillTest.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.Flow
{
    [TestClass]
    public class ShowEmailFlowTests : EmailBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var luisServices = this.Services.LuisServices;
            luisServices.Clear();
            luisServices.Add("email", new MockEmailLuisRecognizer(new ShowEmailUtterances()));
            luisServices.Add("general", new MockGeneralLuisRecognizer());
        }

        [TestMethod]
        public async Task Test_NotSendingEmailWithOrdinalSelection()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_NotSendingEmailWithNumberSelection()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(BaseTestUtterances.NumberOne)
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] AfterSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.SentSuccessfully.Replies, new StringDictionary());
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }

        private string[] ReadOutPrompt()
        {
            return this.ParseReplies(ShowEmailResponses.ReadOutPrompt.Replies, new StringDictionary());
        }

        private string[] ReadOutMorePrompt()
        {
            return this.ParseReplies(ShowEmailResponses.ReadOutMorePrompt.Replies, new StringDictionary());
        }

        private Action<IActivity> AssertSelectOneOfTheMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(ShowEmailResponses.ReadOutMessage.Replies, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
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

        private Action<IActivity> ShowEmailList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ShowEmailPrompt.Replies, new StringDictionary() { { "SearchType", "relevant unread" } }), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 5);
            };
        }

        private Action<IActivity> ShowNextPage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ShowEmailPrompt.Replies, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
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

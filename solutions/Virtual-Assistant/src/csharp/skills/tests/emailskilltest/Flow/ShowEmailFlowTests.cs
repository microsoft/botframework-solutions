using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.ShowEmail.Resources;
using EmailSkillTest.Flow.Fakes;
using EmailSkillTest.Flow.Strings;
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
            var luisServices = this.Services.LocaleConfigurations["en"].LuisServices;
            luisServices.Clear();
            luisServices.Add("email", new MockEmailLuisRecognizer(new ShowEmailUtterances()));
            luisServices.Add("general", new MockGeneralLuisRecognizer());
        }

        [TestMethod]
        public async Task Test_ShowEmail()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailFromSomeone()
        {
            // Setup email data
            var serviceManager = this.ServiceManager as MockServiceManager;
            serviceManager.MockMailService.MyMessages = serviceManager.MockMailService.FakeMyMessages();

            var message = serviceManager.MockMailService.FakeMessage(senderName: ContextStrings.TestRecipient, senderAddress: ContextStrings.TestEmailAdress);
            serviceManager.MockMailService.MyMessages.Add(message);

            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmailsFromTestRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailFromSomeoneList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SelectlWithOrdinal()
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
        public async Task Test_SelectWithNumber()
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

        [TestMethod]
        public async Task Test_ShowEmailThenSayYes()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] NotShowingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.CancellingMessage.Replies, new StringDictionary());
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

        private Action<IActivity> ShowEmailList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ShowEmailPrompt.Replies, new StringDictionary() { { "SearchType", "relevant unread" } }), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 5);
            };
        }

        private Action<IActivity> ShowEmailFromSomeoneList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ShowEmailPrompt.Replies, new StringDictionary() { { "SearchType", "relevant" } }), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
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

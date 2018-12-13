﻿using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Dialogs.DeleteEmail.Resources;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkill.Dialogs.ShowEmail.Resources;
using EmailSkillTest.Flow.Fakes;
using EmailSkillTest.Flow.Strings;
using EmailSkillTest.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Data;
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

            var emailLuisRecognizer = new MockEmailLuisRecognizer(new ShowEmailUtterances());
            emailLuisRecognizer.AddUtteranceManager(new ForwardEmailUtterances());
            emailLuisRecognizer.AddUtteranceManager(new ReplyEmailUtterances());
            emailLuisRecognizer.AddUtteranceManager(new DeleteEmailUtterances());

            luisServices.Add("email", emailLuisRecognizer);
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
                .AssertReplyOneOf(this.ReadOutOnlyOnePrompt())
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

        [TestMethod]
        public async Task Test_ShowEmailThenForwardWithSelection()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(ForwardEmailUtterances.ForwardEmailsToSelection)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(ContextStrings.TestRecipient)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenForwardCurrentSelection()
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
                .Send(ForwardEmailUtterances.ForwardCurrentEmail)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(ContextStrings.TestRecipient)
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenReplyWithSelection()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(ReplyEmailUtterances.ReplyEmailsWithSelection)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenReplyCurrentSelection()
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
                .Send(ReplyEmailUtterances.ReplyCurrentEmail)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectEmailContentMessage())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenDeleteWithSelection()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(DeleteEmailUtterances.DeleteEmailsWithSelection)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.DeleteConfirm())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenDeleteCurrentSelection()
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
                .Send(DeleteEmailUtterances.DeleteCurrentEmail)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.DeleteConfirm())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenGoToTheNextPage()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList(ConfigData.GetInstance().MaxDisplaySize))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.NextPage)
                .AssertReply(this.ShowEmailList(2))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.NextPage)
                .AssertReply(this.ShowEmailList(2))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenGoToPreviousPage()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList(ConfigData.GetInstance().MaxDisplaySize))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.NextPage)
                .AssertReply(this.ShowEmailList(2))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.PreviousPage)
                .AssertReply(this.ShowEmailList(ConfigData.GetInstance().MaxDisplaySize))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.PreviousPage)
                .AssertReply(this.ShowEmailList(ConfigData.GetInstance().MaxDisplaySize))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenReadMore()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList(ConfigData.GetInstance().MaxDisplaySize))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.ReadMore)
                .AssertReply(this.ShowEmailList(ConfigData.GetInstance().MaxDisplaySize - ConfigData.GetInstance().MaxReadSize))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.ReadMore)
                .AssertReply(this.ShowEmailList(2))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailWithZeroItem()
        {
            // Setup email data
            var serviceManager = this.ServiceManager as MockServiceManager;
            serviceManager.MockMailService.MyMessages = serviceManager.MockMailService.FakeMyMessages(0);

            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.EmailNotFoundPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailWithOneItem()
        {
            // Setup email data
            var serviceManager = this.ServiceManager as MockServiceManager;
            serviceManager.MockMailService.MyMessages = serviceManager.MockMailService.FakeMyMessages(1);

            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowEmailList(1))
                .AssertReplyOneOf(this.ReadOutOnlyOnePrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertSelectOneOfTheMessage())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
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

        private string[] ReadOutOnlyOnePrompt()
        {
            return this.ParseReplies(ShowEmailResponses.ReadOutOnlyOnePrompt.Replies, new StringDictionary());
        }

        private string[] ReadOutMorePrompt()
        {
            return this.ParseReplies(ShowEmailResponses.ReadOutMorePrompt.Replies, new StringDictionary());
        }

        private string[] EmailNotFoundPrompt()
        {
            return this.ParseReplies(EmailSharedResponses.EmailNotFound.Replies, new StringDictionary());
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

        private string[] NotSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.CancellingMessage.Replies, new StringDictionary());
        }

        private string[] DeleteConfirm()
        {
            return this.ParseReplies(DeleteEmailResponses.DeleteConfirm.Replies, new StringDictionary());
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

        private Action<IActivity> ShowEmailList(int expectCount = 3)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ShowEmailPrompt.Replies, new StringDictionary() { { "SearchType", "relevant unread" } }), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, expectCount);
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

        private Action<IActivity> ShowEmailWithZeroItems()
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

        private Action<IActivity> AssertComfirmBeforeSendingPrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ConfirmSend.Replies, new StringDictionary()), messageActivity.Text);
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

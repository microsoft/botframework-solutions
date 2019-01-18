using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Dialogs.ConfirmRecipient.Resources;
using EmailSkill.Dialogs.Main.Resources;
using EmailSkill.Dialogs.SendEmail.Resources;
using EmailSkill.Dialogs.Shared.Resources;
using EmailSkillTest.Flow.Fakes;
using EmailSkillTest.Flow.Strings;
using EmailSkillTest.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.Flow
{
    [TestClass]
    public class ConfirmRecipientFlowTests : EmailBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var luisServices = this.Services.LocaleConfigurations["en"].LuisServices;
            luisServices.Clear();
            luisServices.Add("email", new MockEmailLuisRecognizer(new SendEmailUtterances()));
            luisServices.Add("general", new MockGeneralLuisRecognizer());
        }

        [TestMethod]
        public async Task Test_SendToRecipientWhichIsNotFound()
        {
            string testRecipient = ContextStrings.Nobody;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToNobody)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.RecipientNotFoundMessage(recipientDict))
                .Send(GeneralTestUtterances.Cancel)
                .AssertReplyOneOf(this.CancellingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendToRecipientWithExactMatch()
        {
            string testRecipient = ContextStrings.TestRecipient;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(GeneralTestUtterances.Cancel)
                .AssertReplyOneOf(this.CancellingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendToRecipientWithNextPage()
        {
            string testRecipient = ContextStrings.TestRecipientWithDup;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToDupRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CollectRecipients())
                .Send(GeneralTestUtterances.NextPage)
                .AssertReply(this.CollectRecipients())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(GeneralTestUtterances.Cancel)
                .AssertReplyOneOf(this.CancellingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendToRecipientWithPreviousPage()
        {
            string testRecipient = ContextStrings.TestRecipientWithDup;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToDupRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CollectRecipients())
                .Send(GeneralTestUtterances.NextPage)
                .AssertReply(this.CollectRecipients())
                .Send(GeneralTestUtterances.PreviousPage)
                .AssertReply(this.CollectRecipients())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(GeneralTestUtterances.Cancel)
                .AssertReplyOneOf(this.CancellingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SendToRecipientWithReadMore()
        {
            string testRecipient = ContextStrings.TestRecipientWithDup;
            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient } };

            await this.GetTestFlow()
                .Send(SendEmailUtterances.SendEmailToDupRecipient)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CollectRecipients())
                .Send(GeneralTestUtterances.ReadMore)
                .AssertReply(this.CollectRecipients())
                .Send(GeneralTestUtterances.ReadMore)
                .AssertReply(this.CollectRecipients())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.CollectSubjectMessage(recipientDict))
                .Send(GeneralTestUtterances.Cancel)
                .AssertReplyOneOf(this.CancellingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] CancellingMessage()
        {
            return this.ParseReplies(EmailMainResponses.CancelMessage.Replies, new StringDictionary());
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }

        private Action<IActivity> CollectRecipients()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(ConfirmRecipientResponses.ConfirmRecipientLastPage.Replies, new StringDictionary());

                Assert.IsTrue(recipientConfirmedMessage.Length == 1);
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
            };
        }

        private Action<IActivity> CollectSubjectMessage(StringDictionary recipients)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(EmailSharedResponses.RecipientConfirmed.Replies, recipients);
                var noSubjectMessage = this.ParseReplies(SendEmailResponses.NoSubject.Replies, new StringDictionary());

                string[] subjectVerifyInfo = new string[recipientConfirmedMessage.Length * noSubjectMessage.Length];
                int index = -1;
                foreach (var confirmNsg in recipientConfirmedMessage)
                {
                    foreach (var noSubjectMsg in noSubjectMessage)
                    {
                        index++;
                        subjectVerifyInfo[index] = confirmNsg + " " + noSubjectMsg;
                    }
                }

                CollectionAssert.Contains(subjectVerifyInfo, messageActivity.Text);
            };
        }

        private Action<IActivity> RecipientNotFoundMessage(StringDictionary recipients)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientNotFoundMessage = this.ParseReplies(ConfirmRecipientResponses.PromptPersonNotFound.Replies, recipients);

                CollectionAssert.Contains(recipientNotFoundMessage, messageActivity.Text);
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

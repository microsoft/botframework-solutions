﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EmailSkill.Responses.DeleteEmail;
using EmailSkill.Responses.FindContact;
using EmailSkill.Responses.Shared;
using EmailSkill.Responses.ShowEmail;
using EmailSkill.Services;
using EmailSkill.Tests.Flow.Fakes;
using EmailSkill.Tests.Flow.Strings;
using EmailSkill.Tests.Flow.Utterances;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkill.Tests.Flow
{
    [TestClass]
    public class ShowEmailFlowTests : EmailSkillTestBase
    {
        [TestMethod]
        public async Task Test_ShowEmail()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
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
            serviceManager.MailService.MyMessages = serviceManager.MailService.FakeMyMessages();

            var message = serviceManager.MailService.FakeMessage(senderName: ContextStrings.TestRecipient, senderAddress: ContextStrings.TestEmailAdress);
            serviceManager.MailService.MyMessages.Add(message);

            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmailsFromTestRecipient)
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
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.AssertSelectOneOfTheMessage(1))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SelectWithNumber()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(BaseTestUtterances.NumberOne)
                .AssertReply(this.AssertSelectOneOfTheMessage(1))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenSayYes()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertSelectOneOfTheMessage(1))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenForwardWithSelection()
        {
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;
            StringDictionary recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmailAddress } };

            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(ForwardEmailUtterances.ForwardEmailsToSelection)
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(ContextStrings.TestRecipient)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress())
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.CollectEmailContentMessageForForward(testRecipient))
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenForwardCurrentSelection()
        {
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;
            StringDictionary recipientList = new StringDictionary() { { "NameList", testRecipient + ": " + testEmailAddress } };

            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.AssertSelectOneOfTheMessage(1))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(ForwardEmailUtterances.ForwardCurrentEmail)
                .AssertReplyOneOf(this.CollectRecipientsMessage())
                .Send(ContextStrings.TestRecipient)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress())
                .Send(GeneralTestUtterances.Yes)
                .AssertReplyOneOf(this.AddMoreContacts(recipientList))
                .Send(GeneralTestUtterances.No)
                .AssertReply(this.CollectEmailContentMessageForForward(testRecipient))
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenReplyWithSelection()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(ReplyEmailUtterances.ReplyEmailsWithSelection)
                .AssertReplyOneOf(this.CollectEmailContentMessageForReply())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenReplyCurrentSelection()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.AssertSelectOneOfTheMessage(1))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(ReplyEmailUtterances.ReplyCurrentEmail)
                .AssertReplyOneOf(this.CollectEmailContentMessageForReply())
                .Send(ContextStrings.TestContent)
                .AssertReply(this.AssertComfirmBeforeSendingPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenDeleteWithSelection()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(DeleteEmailUtterances.DeleteEmailsWithSelection)
                .AssertReplyOneOf(this.DeleteConfirm())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenDeleteCurrentSelection()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.AssertSelectOneOfTheMessage(1))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(DeleteEmailUtterances.DeleteCurrentEmail)
                .AssertReplyOneOf(this.DeleteConfirm())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotSendingMessage())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenGoToTheNextPage()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList(ConfigData.GetInstance().MaxDisplaySize))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.NextPage)
                .AssertReply(this.ShowEmailList(2, 1))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.NextPage)
                .AssertReply(this.ShowEmailList(2, 2))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenGoToPreviousPage()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList(ConfigData.GetInstance().MaxDisplaySize))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.NextPage)
                .AssertReply(this.ShowEmailList(2, 1))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.PreviousPage)
                .AssertReply(this.ShowEmailList(ConfigData.GetInstance().MaxDisplaySize))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.PreviousPage)
                .AssertReply(this.ShowEmailList(ConfigData.GetInstance().MaxDisplaySize, -1))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenReadMore()
        {
            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList(ConfigData.GetInstance().MaxDisplaySize))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(ShowEmailUtterances.ReadMore)
                .AssertReply(this.ShowEmailList(2, 1))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailWithZeroItem()
        {
            // Setup email data
            var serviceManager = this.ServiceManager as MockServiceManager;
            serviceManager.MailService.MyMessages = serviceManager.MailService.FakeMyMessages(0);

            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReplyOneOf(this.EmailNotFoundPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailWithOneItem()
        {
            // Setup email data
            var serviceManager = this.ServiceManager as MockServiceManager;
            serviceManager.MailService.MyMessages = serviceManager.MailService.FakeMyMessages(1);

            await this.GetTestFlow()
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList(1))
                .AssertReplyOneOf(this.ReadOutOnlyOnePrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertSelectOneOfTheMessage(1))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] NotShowingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.CancellingMessage, new StringDictionary());
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Handoff);
            };
        }

        private string[] ReadOutPrompt()
        {
            return this.ParseReplies(ShowEmailResponses.ReadOutPrompt, new StringDictionary());
        }

        private string[] ReadOutOnlyOnePrompt()
        {
            return this.ParseReplies(ShowEmailResponses.ReadOutOnlyOnePrompt, new StringDictionary());
        }

        private string[] ReadOutMorePrompt()
        {
            return this.ParseReplies(ShowEmailResponses.ReadOutMorePrompt, new StringDictionary());
        }

        private string[] EmailNotFoundPrompt()
        {
            return this.ParseReplies(EmailSharedResponses.EmailNotFound, new StringDictionary());
        }

        private string[] CollectRecipientsMessage()
        {
            return this.ParseReplies(EmailSharedResponses.NoRecipients, new StringDictionary());
        }

        private string[] ConfirmOneNameOneAddress()
        {
            return this.ParseReplies(FindContactResponses.PromptOneNameOneAddress, new StringDictionary() { { "UserName", ContextStrings.TestRecipient }, { "EmailAddress", ContextStrings.TestEmailAdress } });
        }

        private string[] CollectFocusedMessage()
        {
            return this.ParseReplies(EmailSharedResponses.NoFocusMessage, new StringDictionary());
        }

        private string[] CollectEmailContentMessageForReply()
        {
            return this.ParseReplies(EmailSharedResponses.NoEmailContentForReply, new StringDictionary());
        }

        private string[] NotSendingMessage()
        {
            return this.ParseReplies(EmailSharedResponses.CancellingMessage, new StringDictionary());
        }

        private string[] DeleteConfirm()
        {
            return this.ParseReplies(DeleteEmailResponses.DeleteConfirm, new StringDictionary());
        }

        private string[] AddMoreContacts(StringDictionary recipientDict)
        {
            return this.ParseReplies(FindContactResponses.AddMoreContactsPrompt, recipientDict);
        }

        private Action<IActivity> AssertSelectOneOfTheMessage(int selection)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var totalEmails = ((MockServiceManager)this.ServiceManager).MailService.MyMessages;

                var replies = this.ParseReplies(ShowEmailResponses.ReadOutMessage, new StringDictionary()
                {
                    { "EmailDetails", SpeakHelper.ToSpeechEmailDetailString(totalEmails[selection - 1], TimeZoneInfo.Local) },
                });

                CollectionAssert.Contains(replies, messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private Action<IActivity> ShowEmailList(int expectCount = 3, int page = 0)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var prompt = EmailSharedResponses.ShowEmailPrompt;
                if (page == 0)
                {
                    if (expectCount == 1)
                    {
                        prompt = EmailSharedResponses.ShowOneEmailPrompt;
                    }
                }
                else
                {
                    if (expectCount == 1)
                    {
                        prompt = EmailSharedResponses.ShowOneEmailPromptOtherPage;
                    }
                    else
                    {
                        prompt = EmailSharedResponses.ShowEmailPromptOtherPage;
                    }
                }

                var totalEmails = ((MockServiceManager)this.ServiceManager).MailService.MyMessages;
                var showEmails = new List<Message>();

                if (page < 0)
                {
                    var pagingInfo = this.ParseReplies(EmailSharedResponses.FirstPageAlready, new StringDictionary())[0];
                    Assert.IsTrue(messageActivity.Text.StartsWith(pagingInfo));
                }
                else if (page * ConfigData.GetInstance().MaxDisplaySize > totalEmails.Count)
                {
                    var pagingInfo = this.ParseReplies(EmailSharedResponses.LastPageAlready, new StringDictionary())[0];
                    Assert.IsTrue(messageActivity.Text.StartsWith(pagingInfo));
                }
                else
                {
                    for (int i = ConfigData.GetInstance().MaxDisplaySize * page; i < totalEmails.Count; i++)
                    {
                        showEmails.Add(totalEmails[i]);
                    }

                    var replies = this.ParseReplies(prompt, new StringDictionary()
                    {
                        { "TotalCount", totalEmails.Count.ToString() },
                        { "EmailListDetails", SpeakHelper.ToSpeechEmailListString(showEmails, TimeZoneInfo.Local, ConfigData.GetInstance().MaxReadSize) },
                    });

                    CollectionAssert.Contains(replies, messageActivity.Text);
                }
            };
        }

        private Action<IActivity> CollectEmailContentMessageForForward(string userName)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                var noEmailContentMessage = ResponseManager.GetResponse(EmailSharedResponses.NoEmailContentForForward);
                var recipientConfirmedMessage = ResponseManager.GetResponse(EmailSharedResponses.RecipientConfirmed, new StringDictionary() { { "UserName", userName } });
                noEmailContentMessage.Text = recipientConfirmedMessage.Text + " " + noEmailContentMessage.Text;
                noEmailContentMessage.Speak = recipientConfirmedMessage.Speak + " " + noEmailContentMessage.Speak;

                Assert.AreEqual(noEmailContentMessage.Text, messageActivity.Text);
            };
        }

        private Action<IActivity> ShowEmailFromSomeoneList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                var showedItems = ((MockServiceManager)this.ServiceManager).MailService.MyMessages;

                var replies = this.ParseReplies(EmailSharedResponses.ShowEmailPrompt, new StringDictionary()
                {
                    { "TotalCount", "1" },
                    { "EmailListDetails", SpeakHelper.ToSpeechEmailListString(showedItems, TimeZoneInfo.Local, ConfigData.GetInstance().MaxReadSize) },
                });
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private Action<IActivity> ShowEmailWithZeroItems()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ShowEmailPrompt, new StringDictionary() { { "SearchType", "relevant" } }), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private Action<IActivity> ShowNextPage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(this.ParseReplies(EmailSharedResponses.ShowEmailPrompt, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
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
    }
}

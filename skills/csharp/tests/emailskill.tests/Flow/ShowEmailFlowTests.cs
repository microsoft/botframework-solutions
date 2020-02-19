// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmailSkill.Responses.DeleteEmail;
using EmailSkill.Responses.FindContact;
using EmailSkill.Responses.Main;
using EmailSkill.Responses.Shared;
using EmailSkill.Responses.ShowEmail;
using EmailSkill.Tests.Flow.Fakes;
using EmailSkill.Tests.Flow.Strings;
using EmailSkill.Tests.Flow.Utterances;
using EmailSkill.Utilities;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class ShowEmailFlowTests : EmailSkillTestBase
    {
        [TestMethod]
        public async Task Test_ShowEmail()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
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
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(ShowEmailUtterances.ShowEmailsFromTestRecipient)
                .AssertReply(this.ShowEmailFromSomeoneList())
                .AssertReplyOneOf(this.ReadOutOnlyOnePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SelectlWithOrdinal()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(BaseTestUtterances.FirstOne)
                .AssertReply(this.AssertSelectOneOfTheMessage(1))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SelectWithNumber()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(BaseTestUtterances.NumberOne)
                .AssertReply(this.AssertSelectOneOfTheMessage(1))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenSayYes()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList())
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertSelectOneOfTheMessage(1))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenForwardWithSelection()
        {
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;
            object recipientList = new { NameList = testRecipient + ": " + testEmailAddress };

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenForwardCurrentSelection()
        {
            string testRecipient = ContextStrings.TestRecipient;
            string testEmailAddress = ContextStrings.TestEmailAdress;
            object recipientList = new { NameList = testRecipient + ": " + testEmailAddress };

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenReplyWithSelection()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenReplyCurrentSelection()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenDeleteWithSelection()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenDeleteCurrentSelection()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenGoToTheNextPage()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenGoToPreviousPage()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailThenReadMore()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList(ConfigData.GetInstance().MaxDisplaySize))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(ShowEmailUtterances.ReadMore)
                .AssertReply(this.ShowEmailList(2, 1))
                .AssertReplyOneOf(this.ReadOutPrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailWithZeroItem()
        {
            // Setup email data
            var serviceManager = this.ServiceManager as MockServiceManager;
            serviceManager.MailService.MyMessages = serviceManager.MailService.FakeMyMessages(0);

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReplyOneOf(this.EmailNotFoundPrompt())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmailWithOneItem()
        {
            // Setup email data
            var serviceManager = this.ServiceManager as MockServiceManager;
            serviceManager.MailService.MyMessages = serviceManager.MailService.FakeMyMessages(1);

            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(EmailMainResponses.FirstPromptMessage))
                .Send(ShowEmailUtterances.ShowEmails)
                .AssertReply(this.ShowEmailList(1))
                .AssertReplyOneOf(this.ReadOutOnlyOnePrompt())
                .Send(GeneralTestUtterances.Yes)
                .AssertReply(this.AssertSelectOneOfTheMessage(1))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.No)
                .AssertReplyOneOf(this.NotShowingMessage())
                .StartTestAsync();
        }

        private string[] NotShowingMessage()
        {
            return GetTemplates(EmailSharedResponses.CancellingMessage);
        }

        private string[] ReadOutPrompt()
        {
            return GetTemplates(ShowEmailResponses.ReadOutForMultiEmails);
        }

        private string[] ReadOutOnlyOnePrompt()
        {
            return GetTemplates(ShowEmailResponses.ReadOutForOneEmail);
        }

        private string[] ReadOutMorePrompt()
        {
            return GetTemplates(ShowEmailResponses.ReadOutMore);
        }

        private string[] EmailNotFoundPrompt()
        {
            return GetTemplates(EmailSharedResponses.EmailNotFound);
        }

        private string[] CollectRecipientsMessage()
        {
            return GetTemplates(EmailSharedResponses.NoRecipients);
        }

        private string[] ConfirmOneNameOneAddress()
        {
            return GetTemplates(FindContactResponses.PromptOneNameOneAddress, new
            {
                UserName = ContextStrings.TestRecipient,
                EmailAddress = ContextStrings.TestEmailAdress
            });
        }

        private string[] CollectEmailContentMessageForReply()
        {
            return GetTemplates(EmailSharedResponses.NoEmailContentForReply);
        }

        private string[] NotSendingMessage()
        {
            return GetTemplates(EmailSharedResponses.CancellingMessage);
        }

        private string[] DeleteConfirm()
        {
            return GetTemplates(DeleteEmailResponses.DeleteConfirm);
        }

        private string[] AddMoreContacts(object recipientDict)
        {
            return GetTemplates(FindContactResponses.AddMoreContactsPrompt, recipientDict);
        }

        private Action<IActivity> AssertSelectOneOfTheMessage(int selection)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var totalEmails = ((MockServiceManager)this.ServiceManager).MailService.MyMessages;

                var replies = GetTemplates(ShowEmailResponses.ReadOutMessage, new
                {
                    emailDetailsWithoutContent = SpeakHelper.ToSpeechEmailDetailString(totalEmails[selection - 1], TimeZoneInfo.Local)
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
                    var pagingInfo = GetTemplates(EmailSharedResponses.FirstPageAlready)[0];
                    Assert.IsTrue(messageActivity.Text.StartsWith(pagingInfo));
                }
                else if (page * ConfigData.GetInstance().MaxDisplaySize > totalEmails.Count)
                {
                    var pagingInfo = GetTemplates(EmailSharedResponses.LastPageAlready)[0];
                    Assert.IsTrue(messageActivity.Text.StartsWith(pagingInfo));
                }
                else
                {
                    for (int i = ConfigData.GetInstance().MaxDisplaySize * page; i < totalEmails.Count; i++)
                    {
                        showEmails.Add(totalEmails[i]);
                    }

                    var replies = GetTemplates(prompt, new
                    {
                        TotalCount = totalEmails.Count.ToString(),
                        EmailListDetails = SpeakHelper.ToSpeechEmailListString(showEmails, TimeZoneInfo.Local, ConfigData.GetInstance().MaxReadSize)
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

                var noEmailContentMessages = GetTemplates(EmailSharedResponses.NoEmailContentForForward);
                var recipientConfirmedMessages = GetTemplates(EmailSharedResponses.RecipientConfirmed, new { userName = userName });

                var allReply = new List<string>();
                foreach (var recipientConfirmedMessage in recipientConfirmedMessages)
                {
                    foreach (var noEmailContentMessage in noEmailContentMessages)
                    {
                        allReply.Add(recipientConfirmedMessage + " " + noEmailContentMessage);
                    }
                }

                CollectionAssert.Contains(allReply, messageActivity.Text);
            };
        }

        private Action<IActivity> ShowEmailFromSomeoneList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                var showedItems = ((MockServiceManager)this.ServiceManager).MailService.MyMessages;

                Assert.AreEqual(messageActivity.Attachments.Count, 1);
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
    }
}

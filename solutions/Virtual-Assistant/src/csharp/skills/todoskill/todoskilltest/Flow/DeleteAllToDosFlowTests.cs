// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.DeleteToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkillTest.Flow.Fakes;
using ToDoSkillTest.Flow.Utterances;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class DeleteAllToDosFlowTests : ToDoBotTestBase
    {
        [TestMethod]
        public async Task Test_DeleteAllToDoItems()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteAllTasks)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectListType())
                .Send(DeleteToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.CollectConfirmation())
                .Send(MockData.ConfirmYes)
                .AssertReply(this.ShowUpdatedCard())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteAllToDoItems_Confirm_No()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteAllTasks)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectListType())
                .Send(DeleteToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.CollectConfirmation())
                .Send(MockData.ConfirmNo)
                .AssertReply(this.ShowCardOfDeletionRefused())
                .StartTestAsync();
        }

        private string[] CollectListType()
        {
            return this.ParseReplies(DeleteToDoResponses.ListTypePromptForDelete, new StringDictionary());
        }

        private Action<IActivity> CollectConfirmation()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(
                   this.ParseReplies(DeleteToDoResponses.AskDeletionAllConfirmation, new StringDictionary() { { MockData.ListType, MockData.ToDo } }),
                   messageActivity.Text);
            };
        }

        private Action<IActivity> ShowUpdatedCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(1, messageActivity.Attachments.Count);

                CollectionAssert.Contains(
                  this.ParseReplies(DeleteToDoResponses.AfterAllTasksDeleted, new StringDictionary() { { MockData.ListType, MockData.ToDo } }),
                  messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowCardOfDeletionRefused()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(1, messageActivity.Attachments.Count);

                CollectionAssert.Contains(
                  this.ParseReplies(DeleteToDoResponses.DeletionAllConfirmationRefused, new StringDictionary() { { MockData.TaskCount, MockData.MockTaskItems.Count.ToString() }, { MockData.ListType, MockData.ToDo } }),
                  messageActivity.Speak);
            };
        }

        private string[] SettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.SettingUpOutlookMessage, new StringDictionary());
        }

        private string[] AfterSettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.AfterOutlookSetupMessage, new StringDictionary());
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
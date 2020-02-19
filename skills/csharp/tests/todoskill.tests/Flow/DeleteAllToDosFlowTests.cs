// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.DeleteToDo;
using ToDoSkill.Responses.Main;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Tests.Flow.Fakes;
using ToDoSkill.Tests.Flow.Utterances;

namespace ToDoSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class DeleteAllToDosFlowTests : ToDoSkillTestBase
    {
        [TestMethod]
        public async Task Test_DeleteAllToDoItems()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(DeleteToDoFlowTestUtterances.DeleteAllTasks)
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
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(DeleteToDoFlowTestUtterances.DeleteAllTasks)
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
            return GetTemplates(DeleteToDoResponses.ListTypePromptForDelete);
        }

        private Action<IActivity> CollectConfirmation()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(
                 this.AskDeletionAllConfirmation(new
                 {
                     ListType = MockData.ToDo
                 }), messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowUpdatedCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(1, messageActivity.Attachments.Count);

                CollectionAssert.Contains(
                    this.AfterAllTasksDeleted(new
                    {
                        ListType = MockData.ToDo
                    }), messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowCardOfDeletionRefused()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(1, messageActivity.Attachments.Count);

                CollectionAssert.Contains(
                  this.DeletionAllConfirmationRefused(new
                  {
                      TaskCount = MockData.MockTaskItems.Count.ToString(),
                      ListType = MockData.ToDo
                  }), messageActivity.Speak);
            };
        }

        private string[] AskDeletionAllConfirmation(object data)
        {
            return GetTemplates(DeleteToDoResponses.AskDeletionAllConfirmation, data);
        }

        private string[] AfterAllTasksDeleted(object data)
        {
            return GetTemplates(DeleteToDoResponses.AfterAllTasksDeleted, data);
        }

        private string[] DeletionAllConfirmationRefused(object data)
        {
            return GetTemplates(DeleteToDoResponses.DeletionAllConfirmationRefused, data);
        }

        private string[] SettingUpOneNote()
        {
            return GetTemplates(ToDoSharedResponses.SettingUpOutlookMessage);
        }

        private string[] AfterSettingUpOneNote()
        {
            return GetTemplates(ToDoSharedResponses.AfterOutlookSetupMessage);
        }
    }
}
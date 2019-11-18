// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.DeleteToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Tests.Flow.Fakes;
using ToDoSkill.Tests.Flow.Utterances;

namespace ToDoSkill.Tests.Flow
{
    [TestClass]
    public class DeleteToDoFlowTests : ToDoSkillTestBase
    {
        [TestMethod]
        public async Task Test_DeleteToDoItem()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.BaseDeleteTask)
                .AssertReplyOneOf(this.CollectListType())
                .Send(DeleteToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReplyOneOf(this.CollectTaskIndex())
                .Send(DeleteToDoFlowTestUtterances.TaskContent)
                .AssertReply(this.ShowUpdatedToDoCard())
                .AssertReplyOneOf(this.DeleteAnotherTask())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem_By_Specific_Index()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteSpecificTask)
                .AssertReplyOneOf(this.CollectListType())
                .Send(DeleteToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedToDoCard())
                .AssertReplyOneOf(this.DeleteAnotherTask())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem_By_Specific_Index_And_ListType()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteSpecificTaskWithListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedShoppingCard())
                .AssertReplyOneOf(this.DeleteAnotherTask())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem_By_Specific_Content()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteTaskByContent)
                .AssertReplyOneOf(this.CollectListType())
                .Send(DeleteToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedToDoCard())
                .AssertReplyOneOf(this.DeleteAnotherTask())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> ShowUpdatedToDoCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);

                CollectionAssert.Contains(
                  this.AfterTaskDeleted(new
                  {
                      TaskContent = MockData.MockTaskItems[0].Topic,
                      ListType = MockData.ToDo
                  }), messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowUpdatedShoppingCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);

                CollectionAssert.Contains(
                 this.AfterTaskDeleted(new
                 {
                     TaskContent = MockData.MockShoppingItems[1].Topic,
                     ListType = MockData.Shopping
                 }), messageActivity.Speak);
            };
        }

        private string[] AfterTaskDeleted(object data)
        {
            return GetTemplates(DeleteToDoResponses.AfterTaskDeleted, data);
        }

        private string[] CollectTaskIndex()
        {
            return GetTemplates(DeleteToDoResponses.AskTaskIndexForDelete, null);
        }

        private string[] SettingUpOneNote()
        {
            return GetTemplates(ToDoSharedResponses.SettingUpOutlookMessage, null);
        }

        private string[] AfterSettingUpOneNote()
        {
            return GetTemplates(ToDoSharedResponses.AfterOutlookSetupMessage, null);
        }

        private string[] DeleteAnotherTask()
        {
            return GetTemplates(DeleteToDoResponses.DeleteAnotherTaskPrompt, null);
        }

        private string[] ActionEndMessage()
        {
            return GetTemplates(ToDoSharedResponses.ActionEnded, null);
        }

        private string[] CollectListType()
        {
            return GetTemplates(DeleteToDoResponses.ListTypePromptForDelete, null);
        }
    }
}
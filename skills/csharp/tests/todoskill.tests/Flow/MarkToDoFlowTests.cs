// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.Main;
using ToDoSkill.Responses.MarkToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Tests.Flow.Fakes;
using ToDoSkill.Tests.Flow.Utterances;

namespace ToDoSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class MarkToDoFlowTests : ToDoSkillTestBase
    {
        [TestMethod]
        public async Task Test_MarkToDoItem()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(MarkToDoFlowTestUtterances.BaseMarkTask)
                .AssertReplyOneOf(this.CollectListType())
                .Send(MarkToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReplyOneOf(this.CollectTaskIndex())
                .Send(MarkToDoFlowTestUtterances.TaskContent)
                .AssertReply(this.ShowCompleteMessage(0))
                .AssertReply(this.ShowSummary())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_MarkToDoItem_By_Specific_Index()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(MarkToDoFlowTestUtterances.MarkSpecificTaskAsCompleted)
                .AssertReplyOneOf(this.CollectListType())
                .Send(MarkToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowCompleteMessage(1))
                .AssertReply(this.ShowSummary())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_MarkToDoItem_By_Specific_Index_And_ListType()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(MarkToDoFlowTestUtterances.MarkSpecificTaskAsCompletedWithListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowGroceryCompleteMessage(2))
                .AssertReply(this.ShowGrocerySummary())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_MarkToDoItem_By_Specific_Content()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(MarkToDoFlowTestUtterances.MarkTaskAsCompletedByContent)
                .AssertReplyOneOf(this.CollectListType())
                .Send(MarkToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowCompleteMessage(0))
                .AssertReply(this.ShowSummary())
                .StartTestAsync();
        }

        private Action<IActivity> ShowCompleteMessage(int index)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(
                    GetTemplates(MarkToDoResponses.AfterTaskCompleted, new
                    {
                        TaskContent = MockData.MockTaskItems[index].Topic,
                        ListType = MockData.ToDo
                    }), messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowSummary()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(
                      GetTemplates(MarkToDoResponses.AfterCompleteCardSummaryMessageForMultipleTasks, new
                      {
                          AllTasksCount = (MockData.MockTaskItems.Count - 1).ToString(),
                          ListType = MockData.ToDo
                      }), messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowGrocerySummary()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(
                      GetTemplates(MarkToDoResponses.AfterCompleteCardSummaryMessageForMultipleTasks, new
                      {
                          AllTasksCount = (MockData.MockGroceryItems.Count - 1).ToString(),
                          ListType = MockData.Grocery
                      }), messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowGroceryCompleteMessage(int index)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(
                     GetTemplates(MarkToDoResponses.AfterTaskCompleted, new
                     {
                         TaskContent = MockData.MockGroceryItems[index].Topic,
                         ListType = MockData.Grocery
                     }), messageActivity.Speak);
            };
        }

        private string[] CollectListType()
        {
            return GetTemplates(MarkToDoResponses.ListTypePromptForComplete);
        }

        private string[] CollectTaskIndex()
        {
            return GetTemplates(MarkToDoResponses.AskTaskIndexForComplete);
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
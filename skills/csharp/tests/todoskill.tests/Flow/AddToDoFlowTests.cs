// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.AddToDo;
using ToDoSkill.Responses.Main;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Tests.Flow.Fakes;
using ToDoSkill.Tests.Flow.Utterances;

namespace ToDoSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class AddToDoFlowTests : ToDoSkillTestBase
    {
        [TestMethod]
        public async Task Test_AddToDoItem_Prompt_To_Ask_Content()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(AddToDoFlowTestUtterances.BaseAddTask)
                .AssertReplyOneOf(this.CollectToDoContent())
                .Send(AddToDoFlowTestUtterances.TaskContent)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedToDoList())
                .AssertReplyOneOf(this.AddMoreTask(MockData.ToDo))
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AddToDoItem_With_Content()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(AddToDoFlowTestUtterances.AddTaskWithContent)
                .AssertReplyOneOf(this.AskSwitchListType())
                .Send(MockData.ConfirmYes)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedGroceryList())
                .AssertReplyOneOf(this.AddMoreTask(MockData.Grocery))
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AddToDoItem_With_Content_And_ListType()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(AddToDoFlowTestUtterances.AddTaskWithContentAndListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedGroceryList())
                .AssertReplyOneOf(this.AddMoreTask(MockData.Grocery))
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AddToDoItem_With_Content_And_ShopVerb()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(AddToDoFlowTestUtterances.AddTaskWithContentAndShopVerb)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedShoppingList())
                .AssertReplyOneOf(this.AddMoreTask(MockData.Shopping))
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AddToDoItem_With_Content_And_CustomizedListType()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(AddToDoFlowTestUtterances.AddTaskWithContentAndCustomizeListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedCustomizedListTypeList())
                .AssertReplyOneOf(this.AddMoreTask(MockData.CustomizedListType))
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] CollectToDoContent()
        {
            return GetTemplates(AddToDoResponses.AskTaskContentText);
        }

        private string[] SettingUpOneNote()
        {
            return GetTemplates(ToDoSharedResponses.SettingUpOutlookMessage);
        }

        private string[] AfterSettingUpOneNote()
        {
            return GetTemplates(ToDoSharedResponses.AfterOutlookSetupMessage);
        }

        private string[] AskSwitchListType()
        {
            return GetTemplates(AddToDoResponses.SwitchListType, new { ListType = MockData.Grocery });
        }

        private string[] AddMoreTask(string listType)
        {
            return GetTemplates(AddToDoResponses.AddMoreTask, new { ListType = listType });
        }

        private Action<IActivity> ShowUpdatedToDoList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(1, messageActivity.Attachments.Count);

                CollectionAssert.Contains(
                 this.AfterTaskAdded(new
                 {
                     TaskContent = AddToDoFlowTestUtterances.TaskContent,
                     ListType = MockData.ToDo
                 }), messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowUpdatedGroceryList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(1, messageActivity.Attachments.Count);

                CollectionAssert.Contains(
                  this.AfterTaskAdded(new
                  {
                      TaskContent = MockData.GroceryItemEggs,
                      ListType = MockData.Grocery
                  }), messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowUpdatedShoppingList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(1, messageActivity.Attachments.Count);

                CollectionAssert.Contains(
                 this.AfterTaskAdded(new
                 {
                     TaskContent = MockData.ShoppingItemShoes,
                     ListType = MockData.Shopping
                 }), messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowUpdatedCustomizedListTypeList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(1, messageActivity.Attachments.Count);

                CollectionAssert.Contains(
                   this.AfterTaskAdded(new
                   {
                       TaskContent = MockData.CustomizedListTypeItemHistory,
                       ListType = MockData.CustomizedListType
                   }), messageActivity.Speak);
            };
        }

        private string[] AfterTaskAdded(object data)
        {
            return GetTemplates(AddToDoResponses.AfterTaskAdded, data);
        }

        private string[] ActionEndMessage()
        {
            return GetTemplates(ToDoSharedResponses.ActionEnded);
        }
    }
}
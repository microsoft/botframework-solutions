// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.AddToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkillTest.Flow.Fakes;
using ToDoSkillTest.Flow.Utterances;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class AddToDoFlowTests : ToDoBotTestBase
    {
        [TestMethod]
        public async Task Test_AddToDoItem_Prompt_To_Ask_Content()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(AddToDoFlowTestUtterances.BaseAddTask)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
                .Send(AddToDoFlowTestUtterances.AddTaskWithContent)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
                .Send(AddToDoFlowTestUtterances.AddTaskWithContentAndListType)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
                .Send(AddToDoFlowTestUtterances.AddTaskWithContentAndShopVerb)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedShoppingList())
                .AssertReplyOneOf(this.AddMoreTask(MockData.Shopping))
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] CollectToDoContent()
        {
            return this.ParseReplies(AddToDoResponses.AskTaskContentText, new StringDictionary());
        }

        private string[] SettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.SettingUpOutlookMessage, new StringDictionary());
        }

        private string[] AfterSettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.AfterOutlookSetupMessage, new StringDictionary());
        }

        private string[] AskSwitchListType()
        {
            return this.ParseReplies(AddToDoResponses.SwitchListType, new StringDictionary() { { MockData.ListType, MockData.Grocery } });
        }

        private string[] AddMoreTask(string listType)
        {
            return this.ParseReplies(AddToDoResponses.AddMoreTask, new StringDictionary() { { MockData.ListType, listType } });
        }

        private Action<IActivity> ShowUpdatedToDoList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(1, messageActivity.Attachments.Count);

                CollectionAssert.Contains(
                    this.ParseReplies(AddToDoResponses.AfterTaskAdded, new StringDictionary()
                    {
                        { MockData.TaskContent, AddToDoFlowTestUtterances.TaskContent },
                        { MockData.ListType, MockData.ToDo }
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
                    this.ParseReplies(AddToDoResponses.AfterTaskAdded, new StringDictionary()
                    {
                        { MockData.TaskContent, MockData.GroceryItemEggs },
                        { MockData.ListType, MockData.Grocery }
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
                   this.ParseReplies(AddToDoResponses.AfterTaskAdded, new StringDictionary()
                   {
                        { MockData.TaskContent, MockData.ShoppingItemShoes },
                        { MockData.ListType, MockData.Shopping }
                   }), messageActivity.Speak);
            };
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

        private string[] ActionEndMessage()
        {
            return this.ParseReplies(ToDoSharedResponses.ActionEnded, new StringDictionary());
        }
    }
}
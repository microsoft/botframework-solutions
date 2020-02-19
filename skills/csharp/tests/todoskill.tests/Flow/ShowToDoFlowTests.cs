// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.Main;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Responses.ShowToDo;
using ToDoSkill.Tests.Flow.Fakes;
using ToDoSkill.Tests.Flow.Utterances;

namespace ToDoSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class ShowToDoFlowTests : ToDoSkillTestBase
    {
        [TestMethod]
        public async Task Test_ShowToDoItems()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(ShowToDoFlowTestUtterances.ShowToDoList)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowToDoCard())
                .AssertReplyOneOf(this.ReadMoreTasksPrompt())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.FirstReadMoreRefused())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowGroceryItems()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(ShowToDoFlowTestUtterances.ShowGroceryList)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowGroceryCard())
                .AssertReplyOneOf(this.ReadMoreTasksPrompt())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.FirstReadMoreRefused())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowShoppingItems()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(ShowToDoFlowTestUtterances.ShowShoppingList)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowShoppingCard())
                .AssertReplyOneOf(this.ReadMoreTasksPrompt())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.FirstReadMoreRefused())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowCustomizedListTypeItems()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(ShowToDoFlowTestUtterances.ShowCustomizedListTypeList)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowCustomizedListTypeCard())
                .AssertReplyOneOf(this.ReadMoreTasksPrompt())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.FirstReadMoreRefused())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ReadMoreItems()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(ShowToDoFlowTestUtterances.ShowToDoList)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowToDoCard())
                .AssertReplyOneOf(this.ReadMoreTasksPrompt())
                .Send(MockData.ConfirmYes)
                .AssertReply(this.ReadMoreTasksCard())
                .AssertReplyOneOf(this.ReadMoreTasksPrompt2())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmptyList()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ClearAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(ShowToDoFlowTestUtterances.ShowToDoList)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReplyOneOf(this.NoTasksPrompt())
                .StartTestAsync();
        }

        private Action<IActivity> ShowToDoCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(1, messageActivity.Attachments.Count);

                var latestFourTasks = MockData.MockTaskItems[0].Topic + ", " + MockData.MockTaskItems[1].Topic + ", " + MockData.MockTaskItems[2].Topic + " and " + MockData.MockTaskItems[3].Topic;
                var expectedMessage = string.Format(MockData.FirstTaskDetailMessage, MockData.MockTaskItems.Count, MockData.ToDo, MockData.PageSize, latestFourTasks);
                Assert.AreEqual(expectedMessage, messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowGroceryCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);

                var latestFourTasks = MockData.MockGroceryItems[0].Topic + ", " + MockData.MockGroceryItems[1].Topic + ", " + MockData.MockGroceryItems[2].Topic + " and " + MockData.MockGroceryItems[3].Topic;
                var expectedMessage = string.Format(MockData.FirstTaskDetailMessage, MockData.MockGroceryItems.Count, MockData.Grocery, MockData.PageSize, latestFourTasks);
                Assert.AreEqual(expectedMessage, messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowShoppingCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);

                var latestFourTasks = MockData.MockShoppingItems[0].Topic + ", " + MockData.MockShoppingItems[1].Topic + ", " + MockData.MockShoppingItems[2].Topic + " and " + MockData.MockShoppingItems[3].Topic;
                var expectedMessage = string.Format(MockData.FirstTaskDetailMessage, MockData.MockShoppingItems.Count, MockData.Shopping, MockData.PageSize, latestFourTasks);
                Assert.AreEqual(expectedMessage, messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowCustomizedListTypeCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);

                var latestFourTasks = MockData.MockCustomizedListTypeItems[0].Topic + ", " + MockData.MockCustomizedListTypeItems[1].Topic + ", " + MockData.MockCustomizedListTypeItems[2].Topic + " and " + MockData.MockCustomizedListTypeItems[3].Topic;
                var expectedMessage = string.Format(MockData.FirstTaskDetailMessage, MockData.MockCustomizedListTypeItems.Count, MockData.CustomizedListType, MockData.PageSize, latestFourTasks);
                Assert.AreEqual(expectedMessage, messageActivity.Speak);
            };
        }

        private Action<IActivity> ReadMoreTasksCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);

                var nextFourTasks = MockData.MockTaskItems[4].Topic + ", " + MockData.MockTaskItems[5].Topic + ", " + MockData.MockTaskItems[6].Topic + " and " + MockData.MockTaskItems[7].Topic;
                var expectedMessage = string.Format(MockData.NextTaskDetailMessage, nextFourTasks);
                Assert.AreEqual(expectedMessage, messageActivity.Speak);
            };
        }

        private string[] SettingUpOneNote()
        {
            return GetTemplates(ToDoSharedResponses.SettingUpOutlookMessage);
        }

        private string[] AfterSettingUpOneNote()
        {
            return GetTemplates(ToDoSharedResponses.AfterOutlookSetupMessage);
        }

        private string[] NoTasksPrompt()
        {
            return GetTemplates(ShowToDoResponses.NoTasksMessage, new { ListType = MockData.ToDo });
        }

        private string[] ReadMoreTasksPrompt()
        {
            return GetTemplates(ShowToDoResponses.ReadMoreTasksPrompt);
        }

        private string[] ReadMoreTasksPrompt2()
        {
            return GetTemplates(ShowToDoResponses.ReadMoreTasksPrompt2);
        }

        private string[] FirstReadMoreRefused()
        {
            return GetTemplates(ToDoSharedResponses.ActionEnded);
        }

        private string[] ActionEndMessage()
        {
            return GetTemplates(ToDoSharedResponses.ActionEnded);
        }
    }
}
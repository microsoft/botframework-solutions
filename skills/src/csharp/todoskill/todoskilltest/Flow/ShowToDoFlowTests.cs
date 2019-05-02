// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Responses.ShowToDo;
using ToDoSkillTest.Flow.Fakes;
using ToDoSkillTest.Flow.Utterances;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class ShowToDoFlowTests : ToDoBotTestBase
    {
        [TestMethod]
        public async Task Test_ShowToDoItems()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(ShowToDoFlowTestUtterances.ShowToDoList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
                .Send(ShowToDoFlowTestUtterances.ShowGroceryList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
                .Send(ShowToDoFlowTestUtterances.ShowShoppingList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowShoppingCard())
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
                .Send(ShowToDoFlowTestUtterances.ShowToDoList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
                .Send(ShowToDoFlowTestUtterances.ShowToDoList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
            return this.ParseReplies(ToDoSharedResponses.SettingUpOutlookMessage, new StringDictionary());
        }

        private string[] AfterSettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.AfterOutlookSetupMessage, new StringDictionary());
        }

        private string[] NoTasksPrompt()
        {
            return this.ParseReplies(ShowToDoResponses.NoTasksMessage, new StringDictionary() { { MockData.ListType, MockData.ToDo } });
        }

        private string[] ReadMoreTasksPrompt()
        {
            return this.ParseReplies(ShowToDoResponses.ReadMoreTasksPrompt, new StringDictionary());
        }

        private string[] ReadMoreTasksPrompt2()
        {
            return this.ParseReplies(ShowToDoResponses.ReadMoreTasksPrompt2, new StringDictionary());
        }

        private string[] FirstReadMoreRefused()
        {
            return this.ParseReplies(ToDoSharedResponses.ActionEnded, new StringDictionary());
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
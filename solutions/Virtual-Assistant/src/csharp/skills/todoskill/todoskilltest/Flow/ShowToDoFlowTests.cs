// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.Dialogs.ShowToDo.Resources;
using ToDoSkillTest.Flow.Fakes;
using ToDoSkillTest.Flow.Utterances;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class ShowToDoFlowTests : ToDoBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            this.Services.LocaleConfigurations.Add(MockData.LocaleEN, new LocaleConfiguration()
            {
                Locale = MockData.LocaleENUS,
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>()
                {
                    { MockData.LuisGeneral, new MockLuisRecognizer(new GeneralTestUtterances()) },
                    { MockData.LuisToDo, new MockLuisRecognizer(new ShowToDoFlowTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_ShowToDoItems()
        {
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
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
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
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
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
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
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
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
                .AssertReplyOneOf(this.ReadMoreTasksPrompt())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmptyList()
        {
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.ClearAllData);
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
                var responseCard = messageActivity.Attachments[0].Content as AdaptiveCard;
                Assert.IsNotNull(responseCard);
                var adaptiveCardTitle = responseCard.Body[0] as AdaptiveTextBlock;
                Assert.IsNotNull(adaptiveCardTitle);
                var toDoChoices = responseCard.Body[1] as AdaptiveContainer;
                Assert.IsNotNull(toDoChoices);
                var toDoChoiceCount = toDoChoices.Items.Count;
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.CardSummaryMessage.Replies, new StringDictionary() { { MockData.TaskCount, MockData.MockTaskItems.Count.ToString() }, { MockData.ListType, MockData.ToDo } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, MockData.PageSize);

                var latestThreeTasks = MockData.MockTaskItems[0].Topic + ", " + MockData.MockTaskItems[1].Topic + " and " + MockData.MockTaskItems[2].Topic;
                var expectedMessage = string.Format(MockData.FirstTaskDetailMessage, MockData.MockTaskItems.Count, MockData.ToDo, MockData.ReadSize, latestThreeTasks);
                Assert.AreEqual(expectedMessage, responseCard.Speak);
            };
        }

        private Action<IActivity> ShowGroceryCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
                var responseCard = messageActivity.Attachments[0].Content as AdaptiveCard;
                Assert.IsNotNull(responseCard);
                var adaptiveCardTitle = responseCard.Body[0] as AdaptiveTextBlock;
                Assert.IsNotNull(adaptiveCardTitle);
                var toDoChoices = responseCard.Body[1] as AdaptiveContainer;
                Assert.IsNotNull(toDoChoices);
                var toDoChoiceCount = toDoChoices.Items.Count;
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.CardSummaryMessage.Replies, new StringDictionary() { { MockData.TaskCount, MockData.MockGroceryItems.Count.ToString() }, { MockData.ListType, MockData.Grocery } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, MockData.PageSize);

                var latestThreeTasks = MockData.MockGroceryItems[0].Topic + ", " + MockData.MockGroceryItems[1].Topic + " and " + MockData.MockGroceryItems[2].Topic;
                var expectedMessage = string.Format(MockData.FirstTaskDetailMessage, MockData.MockGroceryItems.Count, MockData.Grocery, MockData.ReadSize, latestThreeTasks);
                Assert.AreEqual(expectedMessage, responseCard.Speak);
            };
        }

        private Action<IActivity> ShowShoppingCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
                var responseCard = messageActivity.Attachments[0].Content as AdaptiveCard;
                Assert.IsNotNull(responseCard);
                var adaptiveCardTitle = responseCard.Body[0] as AdaptiveTextBlock;
                Assert.IsNotNull(adaptiveCardTitle);
                var toDoChoices = responseCard.Body[1] as AdaptiveContainer;
                Assert.IsNotNull(toDoChoices);
                var toDoChoiceCount = toDoChoices.Items.Count;
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.CardSummaryMessage.Replies, new StringDictionary() { { MockData.TaskCount, MockData.MockShoppingItems.Count.ToString() }, { MockData.ListType, MockData.Shopping } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, MockData.PageSize);

                var latestThreeTasks = MockData.MockShoppingItems[0].Topic + ", " + MockData.MockShoppingItems[1].Topic + " and " + MockData.MockShoppingItems[2].Topic;
                var expectedMessage = string.Format(MockData.FirstTaskDetailMessage, MockData.MockShoppingItems.Count, MockData.Shopping, MockData.ReadSize, latestThreeTasks);
                Assert.AreEqual(expectedMessage, responseCard.Speak);
            };
        }

        private Action<IActivity> ReadMoreTasksCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
                var responseCard = messageActivity.Attachments[0].Content as AdaptiveCard;
                Assert.IsNotNull(responseCard);
                var adaptiveCardTitle = responseCard.Body[0] as AdaptiveTextBlock;
                Assert.IsNotNull(adaptiveCardTitle);
                var toDoChoices = responseCard.Body[1] as AdaptiveContainer;
                Assert.IsNotNull(toDoChoices);
                var toDoChoiceCount = toDoChoices.Items.Count;
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.CardSummaryMessage.Replies, new StringDictionary() { { MockData.TaskCount, MockData.MockTaskItems.Count.ToString() }, { MockData.ListType, MockData.ToDo } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, MockData.PageSize);

                var nextThreeTasks = MockData.MockTaskItems[3].Topic + ", " + MockData.MockTaskItems[4].Topic + " and " + MockData.MockTaskItems[5].Topic;
                var expectedMessage = string.Format(MockData.NextTaskDetailMessage, MockData.ReadSize, nextThreeTasks);
                Assert.AreEqual(expectedMessage, responseCard.Speak);
            };
        }

        private string[] SettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.SettingUpOutlookMessage.Replies, new StringDictionary());
        }

        private string[] AfterSettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.AfterOutlookSetupMessage.Replies, new StringDictionary());
        }

        private string[] NoTasksPrompt()
        {
            return this.ParseReplies(ShowToDoResponses.NoTasksMessage.Replies, new StringDictionary() { { MockData.ListType, MockData.ToDo } });
        }

        private string[] ReadMoreTasksPrompt()
        {
            return this.ParseReplies(ShowToDoResponses.ReadMoreTasksPrompt.Replies, new StringDictionary());
        }

        private string[] FirstReadMoreRefused()
        {
            return this.ParseReplies(ShowToDoResponses.InstructionMessage.Replies, new StringDictionary());
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Event);
            };
        }

        private string[] ActionEndMessage()
        {
            return this.ParseReplies(ToDoSharedResponses.ActionEnded.Replies, new StringDictionary());
        }
    }
}
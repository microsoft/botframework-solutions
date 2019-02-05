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
        private const int PageSize = 6;

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
            await this.GetTestFlow()
                .Send(ShowToDoFlowTestUtterances.ShowToDoList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowToDoList())
                .AssertReplyOneOf(this.ShowMoreTasksHint())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowGroceryItems()
        {
            await this.GetTestFlow()
                .Send(ShowToDoFlowTestUtterances.ShowGroceryList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowGroceryList())
                .AssertReplyOneOf(this.ShowMoreTasksHint())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowShoppingItems()
        {
            await this.GetTestFlow()
                .Send(ShowToDoFlowTestUtterances.ShowShoppingList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowShoppingList())
                .AssertReplyOneOf(this.ShowMoreTasksHint())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowMoreItems()
        {
            await this.GetTestFlow()
                .Send(ShowToDoFlowTestUtterances.ShowToDoList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowToDoList())
                .AssertReplyOneOf(this.ShowMoreTasksHint())
                .AssertReply(this.ActionEndMessage())
                .Send(GeneralTestUtterances.ShowNext)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowNextTasksCard())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowPreviousItems()
        {
            await this.GetTestFlow()
                .Send(ShowToDoFlowTestUtterances.ShowToDoList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowToDoList())
                .AssertReplyOneOf(this.ShowMoreTasksHint())
                .AssertReply(this.ActionEndMessage())
                .Send(GeneralTestUtterances.ShowNext)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowNextTasksCard())
                .AssertReply(this.ActionEndMessage())
                .Send(GeneralTestUtterances.ShowPrevious)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowPreviousTasksCard())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ReadMoreItems()
        {
            await this.GetTestFlow()
                .Send(ShowToDoFlowTestUtterances.ShowToDoList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowToDoList())
                .AssertReplyOneOf(this.ShowMoreTasksHint())
                .AssertReply(this.ActionEndMessage())
                .Send(GeneralTestUtterances.ReadMore)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ReadMoreTasksCard())
                .AssertReply(this.ActionEndMessage())
                .Send(GeneralTestUtterances.ReadMore)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ReadMoreToNextPageCard())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ShowEmptyTasks()
        {
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.KeepZeroItem);
            await this.GetTestFlow()
                .Send(ShowToDoFlowTestUtterances.ShowToDoList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReplyOneOf(this.NoTasksPrompt())
                .Send("yes")
                .AssertReplyOneOf(this.CollectToDoContent())
                .Send(AddToDoFlowTestUtterances.TaskContent)
                .AssertReply(this.ShowUpdatedToDoList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> ShowToDoList()
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
                var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.ShowToDoTasks);
                CollectionAssert.Contains(
                    this.ParseReplies(response.Replies, new StringDictionary() { { MockData.TaskCount, MockData.MockTaskItems.Count.ToString() }, { MockData.ListType, MockData.ToDo } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
            };
        }

        private Action<IActivity> ShowGroceryList()
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
                var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.ShowToDoTasks);
                CollectionAssert.Contains(
                    this.ParseReplies(response.Replies, new StringDictionary() { { MockData.TaskCount, MockData.MockGroceryItems.Count.ToString() }, { MockData.ListType, MockData.Grocery } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
            };
        }

        private Action<IActivity> ShowShoppingList()
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
                var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.ShowToDoTasks);
                CollectionAssert.Contains(
                    this.ParseReplies(response.Replies, new StringDictionary() { { MockData.TaskCount, MockData.MockShoppingItems.Count.ToString() }, { MockData.ListType, MockData.Shopping } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
            };
        }

        private Action<IActivity> ShowNextTasksCard()
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
                var response = ResponseManager.GetResponseTemplate(ShowToDoResponses.ShowNextToDoTasks);
                CollectionAssert.Contains(
                    this.ParseReplies(response.Replies, new StringDictionary() { }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, MockData.MockTaskItems.Count - PageSize);
            };
        }

        private Action<IActivity> ShowPreviousTasksCard()
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
                var response = ResponseManager.GetResponseTemplate(ShowToDoResponses.ShowPreviousToDoTasks);
                CollectionAssert.Contains(
                    this.ParseReplies(response.Replies, new StringDictionary() { }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
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
                var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.ShowToDoTasks);
                CollectionAssert.Contains(
                    this.ParseReplies(response.Replies, new StringDictionary() { { MockData.TaskCount, MockData.MockTaskItems.Count.ToString() }, { MockData.ListType, MockData.ToDo } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
                var speak = responseCard.Speak;
                Assert.IsTrue(speak.Contains(MockData.MockTaskItems[3].Topic, StringComparison.InvariantCultureIgnoreCase));
                Assert.IsFalse(speak.Contains(MockData.MockTaskItems[2].Topic, StringComparison.InvariantCultureIgnoreCase));
            };
        }

        private Action<IActivity> ReadMoreToNextPageCard()
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
                var response = ResponseManager.GetResponseTemplate(ShowToDoResponses.ShowNextToDoTasks);
                CollectionAssert.Contains(
                    this.ParseReplies(response.Replies, new StringDictionary() { }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, MockData.MockTaskItems.Count - PageSize);
            };
        }

        private Action<IActivity> ShowUpdatedToDoList()
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
                var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.ShowToDoTasks);
                CollectionAssert.Contains(
                    this.ParseReplies(response.Replies, new StringDictionary() { { MockData.TaskCount, "1" }, { MockData.ListType, MockData.ToDo } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, 1);
                var columnSet = toDoChoices.Items[0] as AdaptiveColumnSet;
                Assert.IsNotNull(columnSet);
                var column = columnSet.Columns[1] as AdaptiveColumn;
                Assert.IsNotNull(column);
                var content = column.Items[0] as AdaptiveTextBlock;
                Assert.IsNotNull(content);
                Assert.AreEqual(content.Text, AddToDoFlowTestUtterances.TaskContent);
            };
        }

        private string[] SettingUpOneNote()
        {
            var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.SettingUpOutlookMessage);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] AfterSettingUpOneNote()
        {
            var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.AfterOutlookSetupMessage);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] NoTasksPrompt()
        {
            var response = ResponseManager.GetResponseTemplate(ShowToDoResponses.NoToDoTasksPrompt);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] CollectToDoContent()
        {
            var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.AskToDoContentText);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] ShowMoreTasksHint()
        {
            var response = ResponseManager.GetResponseTemplate(ShowToDoResponses.ShowingMoreTasks);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Event);
            };
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.Dialogs.ShowToDo.Resources;
using ToDoSkillTest.Fakes;
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
            this.Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>()
                {
                    { "general", new MockLuisRecognizer(new GeneralTestUtterances()) },
                    { "todo", new MockLuisRecognizer(new ShowToDoFlowTestUtterances()) }
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
            (this.ToDoService as MockToDoService).ChangeData(DataOperationType.OperationType.KeepZeroItem);
            await this.GetTestFlow()
                .Send(ShowToDoFlowTestUtterances.ShowToDoList)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
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
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", MockData.MockTaskItems.Count.ToString() } }),
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
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", MockData.MockGroceryItems.Count.ToString() } }),
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
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", MockData.MockShoppingItems.Count.ToString() } }),
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
                CollectionAssert.Contains(
                    this.ParseReplies(ShowToDoResponses.ShowNextToDoTasks.Replies, new StringDictionary() { }),
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
                CollectionAssert.Contains(
                    this.ParseReplies(ShowToDoResponses.ShowPreviousToDoTasks.Replies, new StringDictionary() { }),
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
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", MockData.MockTaskItems.Count.ToString() } }),
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
                CollectionAssert.Contains(
                    this.ParseReplies(ShowToDoResponses.ShowNextToDoTasks.Replies, new StringDictionary() { }),
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
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", "1" } }),
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
            return this.ParseReplies(ToDoSharedResponses.SettingUpOneNoteMessage.Replies, new StringDictionary());
        }

        private string[] NoTasksPrompt()
        {
            return this.ParseReplies(ShowToDoResponses.NoToDoTasksPrompt.Replies, new StringDictionary());
        }

        private string[] CollectToDoContent()
        {
            return this.ParseReplies(ToDoSharedResponses.AskToDoContentText.Replies, new StringDictionary());
        }

        private string[] ShowMoreTasksHint()
        {
            return this.ParseReplies(ShowToDoResponses.ShowingMoreTasks.Replies, new StringDictionary());
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

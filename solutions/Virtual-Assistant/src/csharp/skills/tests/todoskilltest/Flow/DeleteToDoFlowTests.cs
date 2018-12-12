// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Dialogs.DeleteToDo.Resources;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkillTest.Fakes;
using ToDoSkillTest.Flow.Fakes;
using ToDoSkillTest.Flow.Utterances;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class DeleteToDoFlowTests : ToDoBotTestBase
    {
        private const int PageSize = 6;

        [TestInitialize]
        public void SetupLuisService()
        {
            this.Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, IRecognizer>()
                {
                    { "general", new MockLuisRecognizer(new GeneralTestUtterances()) },
                    { "todo", new MockLuisRecognizer(new DeleteToDoFlowTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem()
        {
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.BaseDeleteTask)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.CollectTaskIndex())
                .Send(DeleteToDoFlowTestUtterances.TaskContent)
                .AssertReplyOneOf(this.CollectConfirmationForToDo())
                .Send("yes")
                .AssertReply(this.AfterTaskDeletedCardMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem_By_Specific_Index()
        {
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteSpecificTask)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.CollectConfirmationForToDo())
                .Send("yes")
                .AssertReply(this.AfterTaskDeletedCardMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem_By_Specific_Index_And_ListType()
        {
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteSpecificTaskWithListType)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.CollectConfirmationForShopping())
                .Send("yes")
                .AssertReply(this.AfterShoppingItemDeletedCardMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem_By_Specific_Content()
        {
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteTaskByContent)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.CollectConfirmationForToDo())
                .Send("yes")
                .AssertReply(this.AfterTaskDeletedCardMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem_Check_Empty_List()
        {
            (this.ToDoService as MockToDoService).ChangeData(DataOperationType.OperationType.KeepOneItem);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteSpecificTask)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.CollectConfirmationForToDo())
                .Send("yes")
                .AssertReply(this.AfterLastTaskDeletedMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem_Confirm_No()
        {
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteSpecificTask)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.CollectConfirmationForToDo())
                .Send("no")
                .AssertReplyOneOf(this.AfterDeletionRejectedMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> AfterTaskDeletedCardMessage()
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
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", (MockData.MockTaskItems.Count - 1).ToString() } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
                var columnSet = toDoChoices.Items[0] as AdaptiveColumnSet;
                Assert.IsNotNull(columnSet);
                var column = columnSet.Columns[1] as AdaptiveColumn;
                Assert.IsNotNull(column);
                var content = column.Items[0] as AdaptiveTextBlock;
                Assert.IsNotNull(content);
                Assert.AreEqual(content.Text, MockData.MockTaskItems[1].Topic);
                var speak = string.Format("I have deleted the item {0} for you.You have {1} items on your list:", MockData.MockTaskItems[0].Topic, MockData.MockTaskItems.Count - 1);
                Assert.AreEqual(speak, responseCard.Speak);
            };
        }

        private Action<IActivity> AfterShoppingItemDeletedCardMessage()
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
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", (MockData.MockShoppingItems.Count - 1).ToString() } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
                var columnSet = toDoChoices.Items[0] as AdaptiveColumnSet;
                Assert.IsNotNull(columnSet);
                var column = columnSet.Columns[1] as AdaptiveColumn;
                Assert.IsNotNull(column);
                var content = column.Items[0] as AdaptiveTextBlock;
                Assert.IsNotNull(content);
                Assert.AreEqual(content.Text, MockData.MockShoppingItems[0].Topic);
                var speak = string.Format("I have deleted the item {0} for you.You have {1} items on your list:", MockData.MockShoppingItems[1].Topic, MockData.MockShoppingItems.Count - 1);
                Assert.AreEqual(speak, responseCard.Speak);
            };
        }

        private Action<IActivity> AfterLastTaskDeletedMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var response = string.Format("I have deleted the item {0} for you. You have {1} items on your list.", MockData.MockTaskItems[0].Topic, 0);
                Assert.AreEqual(response, messageActivity.Text);
            };
        }

        private string[] AfterDeletionRejectedMessage()
        {
            return this.ParseReplies(ToDoSharedResponses.ActionEnded.Replies, new StringDictionary() { });
        }

        private string[] CollectTaskIndex()
        {
            return this.ParseReplies(ToDoSharedResponses.AskToDoTaskIndex.Replies, new StringDictionary());
        }

        private string[] CollectConfirmationForToDo()
        {
            return this.ParseReplies(DeleteToDoResponses.AskDeletionConfirmation.Replies, new StringDictionary() { { "toDoTask", MockData.MockTaskItems[0].Topic } });
        }

        private string[] CollectConfirmationForShopping()
        {
            return this.ParseReplies(DeleteToDoResponses.AskDeletionConfirmation.Replies, new StringDictionary() { { "toDoTask", MockData.MockShoppingItems[1].Topic } });
        }

        private string[] SettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.SettingUpOneNoteMessage.Replies, new StringDictionary());
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

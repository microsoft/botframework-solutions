// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Dialogs.DeleteToDo.Resources;
using ToDoSkill.Dialogs.Main.Resources;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.Dialogs.ShowToDo.Resources;
using ToDoSkillTest.Flow.Fakes;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class DeleteToDoFlowTests : ToDoBotTestBase
    {
        private const int PageSize = 6;

        [TestMethod]
        public async Task Test_DeleteToDoItem()
        {
            await this.GetTestFlow()
                .Send("Show my todos")
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReply(this.ShowToDoList())
                .AssertReplyOneOf(this.ShowMoreTasks())
                .Send("Delete the first task")
                .AssertReplyOneOf(this.CollectConfirmationForToDo())
                .Send("yes")
                .AssertReply(this.AfterTaskDeletedCardMessage())
                .AssertReplyOneOf(this.AfterDialogCompleted())
                .Send("Delete the second task in my shopping list")
                .AssertReplyOneOf(this.CollectConfirmationForShopping())
                .Send("yes")
                .AssertReply(this.AfterShoppingItemDeletedCardMessage())
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
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", FakeData.FakeTaskItems.Count.ToString() } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
            };
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
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", (FakeData.FakeTaskItems.Count - 1).ToString() } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
                var columnSet = toDoChoices.Items[0] as AdaptiveColumnSet;
                Assert.IsNotNull(columnSet);
                var column = columnSet.Columns[1] as AdaptiveColumn;
                Assert.IsNotNull(column);
                var content = column.Items[0] as AdaptiveTextBlock;
                Assert.IsNotNull(content);
                Assert.AreEqual(content.Text, FakeData.FakeTaskItems[1].Topic);
                var speak = string.Format("I have deleted the item {0} for you.You have {1} items on your list:", FakeData.FakeTaskItems[0].Topic, FakeData.FakeTaskItems.Count - 1);
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
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", (FakeData.FakeShoppingItems.Count - 1).ToString() } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
                var columnSet = toDoChoices.Items[0] as AdaptiveColumnSet;
                Assert.IsNotNull(columnSet);
                var column = columnSet.Columns[1] as AdaptiveColumn;
                Assert.IsNotNull(column);
                var content = column.Items[0] as AdaptiveTextBlock;
                Assert.IsNotNull(content);
                Assert.AreEqual(content.Text, FakeData.FakeShoppingItems[0].Topic);
                var speak = string.Format("I have deleted the item {0} for you.You have {1} items on your list:", FakeData.FakeShoppingItems[1].Topic, FakeData.FakeShoppingItems.Count - 1);
                Assert.AreEqual(speak, responseCard.Speak);
            };
        }

        private string[] CollectConfirmationForToDo()
        {
            return this.ParseReplies(DeleteToDoResponses.AskDeletionConfirmation.Replies, new StringDictionary() { { "toDoTask", FakeData.FakeTaskItems[0].Topic } });
        }

        private string[] CollectConfirmationForShopping()
        {
            return this.ParseReplies(DeleteToDoResponses.AskDeletionConfirmation.Replies, new StringDictionary() { { "toDoTask", FakeData.FakeShoppingItems[1].Topic } });
        }

        private string[] SettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.SettingUpOneNoteMessage.Replies, new StringDictionary());
        }

        private string[] ShowMoreTasks()
        {
            return this.ParseReplies(ShowToDoResponses.ShowingMoreTasks.Replies, new StringDictionary());
        }

        private string[] AfterDialogCompleted()
        {
            return this.ParseReplies(ToDoSharedResponses.ActionEnded.Replies, new StringDictionary());
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Dialogs.DeleteToDo.Resources;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.Dialogs.ShowToDo.Resources;
using ToDoSkillTest.Flow.Fakes;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class DeleteAllToDosFlowTests : ToDoBotTestBase
    {
        [TestMethod]
        public async Task Test_DeleteAllToDoItems()
        {
            await this.GetTestFlow()
                .Send("Show my todos")
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReply(this.ShowToDoList())
                .AssertReplyOneOf(this.ShowMoreTasks())
                .Send("Remove all my tasks")
                .AssertReply(this.CollectConfirmation())
                .Send("yes")
                .AssertReply(this.AfterAllTasksDeletedTextMessage())
                .StartTestAsync();
        }

        private Action<IActivity> ShowToDoList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
                var responseCard = messageActivity.Attachments[0].Content as AdaptiveCard;
                var adaptiveCardTitle = responseCard.Body[0] as AdaptiveTextBlock;
                var toDoChoices = responseCard.Body[1] as AdaptiveChoiceSetInput;
                var toDoChoiceCount = toDoChoices.Choices.Count;
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", FakeData.FakeTaskItems.Count.ToString() } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, 5);
            };
        }

        private Action<IActivity> CollectConfirmation()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(
                   this.ParseReplies(DeleteToDoResponses.AskDeletionAllConfirmation.Replies, new StringDictionary()),
                   messageActivity.Text);
            };
        }

        private Action<IActivity> AfterAllTasksDeletedTextMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(
                   this.ParseReplies(DeleteToDoResponses.AfterAllTasksDeleted.Replies, new StringDictionary()),
                   messageActivity.Text);
            };
        }

        private string[] SettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.SettingUpOneNoteMessage.Replies, new StringDictionary());
        }

        private string[] ShowMoreTasks()
        {
            return this.ParseReplies(ShowToDoResponses.ShowingMoreTasks.Replies, new StringDictionary());
        }
    }
}

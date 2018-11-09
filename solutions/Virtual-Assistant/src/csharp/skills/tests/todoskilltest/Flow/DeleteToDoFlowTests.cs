// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkillTest.Flow
{
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

    [TestClass]
    public class DeleteToDoFlowTests : ToDoBotTestBase
    {
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
        }

        [TestMethod]
        public async Task EndToEnd()
        {
            await this.GetTestFlow()
                .Send("Show my to dos")
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReply(this.ShowToDoList())
                .AssertReplyOneOf(this.ShowMoreTasks())
                .Send("Delete the first task")
                .AssertReplyOneOf(this.CollectConfirmation())
                .Send("yes")
                .AssertReply(this.AfterTaskDeletedCardMessage())
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
       
        private Action<IActivity> AfterTaskDeletedCardMessage()
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
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", (FakeData.FakeTaskItems.Count - 1).ToString() } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, 5);
                var toDoChoice = toDoChoices.Choices[0];
                Assert.AreEqual(toDoChoice.Title, FakeData.FakeTaskItems[1].Topic);
            };
        }

        private string[] CollectConfirmation()
        {
            return this.ParseReplies(DeleteToDoResponses.AskDeletionConfirmation.Replies, new StringDictionary() { { "toDoTask", FakeData.FakeTaskItems[0].Topic } });
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

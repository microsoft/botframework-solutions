// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Dialogs.Main.Resources;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkillTest.Flow.Fakes;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class AddToDoFlowTests : ToDoBotTestBase
    {
        [TestMethod]
        public async Task Test_AddToDoItem()
        {
            await this.GetTestFlow()
                .Send("Add a task")
                .AssertReplyOneOf(this.CollectToDoContent())
                .Send("Test Content")
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReply(this.ShowUpdatedToDoList())
                .StartTestAsync();
        }

        private string[] CollectToDoContent()
        {
            return this.ParseReplies(ToDoSharedResponses.AskToDoContentText.Replies, new StringDictionary());
        }

        private string[] SettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.SettingUpOneNoteMessage.Replies, new StringDictionary());
        }

        private Action<IActivity> ShowUpdatedToDoList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(1, messageActivity.Attachments.Count);
                var responseCard = messageActivity.Attachments[0].Content as AdaptiveCard;
                var adaptiveCardTitle = responseCard.Body[0] as AdaptiveTextBlock;
                var toDoChoices = responseCard.Body[1] as AdaptiveChoiceSetInput;
                var toDoChoiceCount = toDoChoices.Choices.Count;
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", (FakeData.FakeTaskItems.Count + 1).ToString() } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, 5);
                var toDoChoice = toDoChoices.Choices[0];
                Assert.AreEqual(toDoChoice.Title, "Test Content");
            };
        }
    }
}

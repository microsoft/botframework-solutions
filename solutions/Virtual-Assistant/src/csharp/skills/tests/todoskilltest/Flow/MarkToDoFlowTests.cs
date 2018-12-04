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
using ToDoSkill.Dialogs.ShowToDo.Resources;
using ToDoSkillTest.Flow.Fakes;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class MarkToDoFlowTests : ToDoBotTestBase
    {
        private const int PageSize = 6;

        [TestMethod]
        public async Task Test_MarkToDoItem()
        {
            await this.GetTestFlow()
                .Send("Show my todos")
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReply(this.ShowToDoList())
                .AssertReplyOneOf(this.ShowMoreTasks())
                .Send("mark the second task as completed")
                .AssertReply(this.AfterTaskMarkedCardMessage())
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
                var toDoChoices = responseCard.Body[1] as AdaptiveContainer;
                var toDoChoiceCount = toDoChoices.Items.Count;
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", FakeData.FakeTaskItems.Count.ToString() } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
            };
        }

        private Action<IActivity> AfterTaskMarkedCardMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
                var responseCard = messageActivity.Attachments[0].Content as AdaptiveCard;
                var adaptiveCardTitle = responseCard.Body[0] as AdaptiveTextBlock;
                var toDoChoices = responseCard.Body[1] as AdaptiveContainer;
                var toDoChoiceCount = toDoChoices.Items.Count;
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", FakeData.FakeTaskItems.Count.ToString() } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
                Assert.AreEqual(((toDoChoices.Items[1] as AdaptiveColumnSet).Columns[0].Items[0] as AdaptiveImage).UrlString, "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAABGdBTUEAALGPC/xhBQAAACBjSFJNAAB6JgAAgIQAAPoAAACA6AAAdTAAAOpgAAA6mAAAF3CculE8AAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAAmJLR0QAAKqNIzIAAAAHdElNRQfiDAMKKhgxjYNuAAAAJXRFWHRkYXRlOmNyZWF0ZQAyMDE4LTEyLTAzVDEwOjQyOjI0KzAxOjAw3NT2SAAAACV0RVh0ZGF0ZTptb2RpZnkAMjAxOC0xMi0wM1QxMDo0MjoyNCswMTowMK2JTvQAAAAZdEVYdFNvZnR3YXJlAHd3dy5pbmtzY2FwZS5vcmeb7jwaAAAA/0lEQVQ4T6WTzQmEMBCFx0XQErypHXj0KliCPYgNiUcLsAev3uxAvVqComTzhrgu/i1hPxjykuG9SBJJSMIwFESkVfAAA6JpGoqiiEzTRKDs32MYBi3LQnVdk/TympBmTtMBHnhfSMDOumweDpCBPHliXVcax1HNdg8H/GKaJt7RcRy1svMzYJ5nsm2bte/7PH5zCijLknArAGbLsljD3HUd6yMijmM+WYA5qqqqj/Y8T3V34EHv9AVZlvGYJAmP0kx937O+4hSQ5zmlacradd1HM7g8xKIoqG1bGoZBrdzDAXieR4IgUOqazcMBeNu6bJ7/fyZcCUJkT6vgEUKINxqN2iFI/P1RAAAAAElFTkSuQmCC");
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

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
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkillTest.Flow.Fakes;
using ToDoSkillTest.Flow.Utterances;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class AddToDoFlowTests : ToDoBotTestBase
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
                    { "general", new MockLuisRecognizer() },
                    { "todo", new MockLuisRecognizer(new AddToDoFlowTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_AddToDoItem()
        {
            await this.GetTestFlow()
                .Send(AddToDoFlowTestUtterances.BaseAddTask)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectToDoContent())
                .Send(AddToDoFlowTestUtterances.TaskContent)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReply(this.ShowUpdatedToDoList())
                .AssertReply(this.ActionEndMessage())
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
                Assert.IsNotNull(responseCard);
                var adaptiveCardTitle = responseCard.Body[0] as AdaptiveTextBlock;
                Assert.IsNotNull(adaptiveCardTitle);
                var toDoChoices = responseCard.Body[1] as AdaptiveContainer;
                Assert.IsNotNull(toDoChoices);
                var toDoChoiceCount = toDoChoices.Items.Count;
                CollectionAssert.Contains(
                    this.ParseReplies(ToDoSharedResponses.ShowToDoTasks.Replies, new StringDictionary() { { "taskCount", (MockData.MockTaskItems.Count + 1).ToString() } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
                var columnSet = toDoChoices.Items[0] as AdaptiveColumnSet;
                Assert.IsNotNull(columnSet);
                var column = columnSet.Columns[1] as AdaptiveColumn;
                Assert.IsNotNull(column);
                var content = column.Items[0] as AdaptiveTextBlock;
                Assert.IsNotNull(content);
                Assert.AreEqual(content.Text, AddToDoFlowTestUtterances.TaskContent);
            };
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

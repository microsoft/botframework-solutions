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
            this.Services.LocaleConfigurations.Add(MockData.LocaleEN, new LocaleConfiguration()
            {
                Locale = MockData.LocaleENUS,
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>()
                {
                    { MockData.LuisGeneral, new MockLuisRecognizer(new GeneralTestUtterances()) },
                    { MockData.LuisToDo, new MockLuisRecognizer(new AddToDoFlowTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_AddToDoItem_Prompt_To_Ask_Content()
        {
            await this.GetTestFlow()
                .Send(AddToDoFlowTestUtterances.BaseAddTask)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectToDoContent())
                .Send(AddToDoFlowTestUtterances.TaskContent)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedToDoList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AddToDoItem_With_Content()
        {
            await this.GetTestFlow()
                .Send(AddToDoFlowTestUtterances.AddTaskWithContent)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskSwitchListType())
                .Send("yes")
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedGroceryList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AddToDoItem_With_Content_And_ListType()
        {
            await this.GetTestFlow()
                .Send(AddToDoFlowTestUtterances.AddTaskWithContentAndListType)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedGroceryList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AddToDoItem_With_Content_And_ShopVerb()
        {
            await this.GetTestFlow()
                .Send(AddToDoFlowTestUtterances.AddTaskWithContentAndShopVerb)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedShoppingList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] CollectToDoContent()
        {
            var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.AskToDoContentText);
            return this.ParseReplies(response.Replies, new StringDictionary());
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

        private string[] AskSwitchListType()
        {
            var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.SwitchListType);
            return this.ParseReplies(response.Replies, new StringDictionary() { { MockData.ListType, MockData.Grocery } });
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
                    this.ParseReplies(response.Replies, new StringDictionary() { { MockData.TaskCount, (MockData.MockTaskItems.Count + 1).ToString() }, { MockData.ListType, MockData.ToDo } }),
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

        private Action<IActivity> ShowUpdatedGroceryList()
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
                    this.ParseReplies(response.Replies, new StringDictionary() { { MockData.TaskCount, (MockData.MockGroceryItems.Count + 1).ToString() }, { MockData.ListType, MockData.Grocery } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
                var columnSet = toDoChoices.Items[0] as AdaptiveColumnSet;
                Assert.IsNotNull(columnSet);
                var column = columnSet.Columns[1] as AdaptiveColumn;
                Assert.IsNotNull(column);
                var content = column.Items[0] as AdaptiveTextBlock;
                Assert.IsNotNull(content);
                Assert.AreEqual("eggs", content.Text);
            };
        }

        private Action<IActivity> ShowUpdatedShoppingList()
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
                    this.ParseReplies(response.Replies, new StringDictionary() { { MockData.TaskCount, (MockData.MockShoppingItems.Count + 1).ToString() }, { MockData.ListType, MockData.Shopping } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
                var columnSet = toDoChoices.Items[0] as AdaptiveColumnSet;
                Assert.IsNotNull(columnSet);
                var column = columnSet.Columns[1] as AdaptiveColumn;
                Assert.IsNotNull(column);
                var content = column.Items[0] as AdaptiveTextBlock;
                Assert.IsNotNull(content);
                Assert.AreEqual("purchase shoes", content.Text);
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
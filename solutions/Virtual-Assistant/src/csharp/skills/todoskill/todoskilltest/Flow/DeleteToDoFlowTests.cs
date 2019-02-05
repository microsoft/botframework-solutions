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
using ToDoSkill.Dialogs.DeleteToDo.Resources;
using ToDoSkill.Dialogs.Shared.Resources;
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
            this.Services.LocaleConfigurations.Add(MockData.LocaleEN, new LocaleConfiguration()
            {
                Locale = MockData.LocaleENUS,
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>()
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
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
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
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
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
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
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
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReplyOneOf(this.CollectConfirmationForToDo())
                .Send("yes")
                .AssertReply(this.AfterTaskDeletedCardMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem_Check_Empty_List()
        {
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.KeepOneItem);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteSpecificTask)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
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
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
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
                var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.ShowToDoTasks);
                CollectionAssert.Contains(
                    this.ParseReplies(response.Replies, new StringDictionary() { { MockData.TaskCount, (MockData.MockTaskItems.Count - 1).ToString() }, { MockData.ListType, MockData.ToDo } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
                var columnSet = toDoChoices.Items[0] as AdaptiveColumnSet;
                Assert.IsNotNull(columnSet);
                var column = columnSet.Columns[1] as AdaptiveColumn;
                Assert.IsNotNull(column);
                var content = column.Items[0] as AdaptiveTextBlock;
                Assert.IsNotNull(content);
                Assert.AreEqual(content.Text, MockData.MockTaskItems[1].Topic);
                var speak = string.Format(MockData.AfterDeleteTaskMessage, MockData.MockTaskItems[0].Topic, MockData.MockTaskItems.Count - 1, MockData.ToDo);
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
                var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.ShowToDoTasks);
                CollectionAssert.Contains(
                    this.ParseReplies(response.Replies, new StringDictionary() { { MockData.TaskCount, (MockData.MockShoppingItems.Count - 1).ToString() }, { MockData.ListType, MockData.Shopping } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, PageSize);
                var columnSet = toDoChoices.Items[0] as AdaptiveColumnSet;
                Assert.IsNotNull(columnSet);
                var column = columnSet.Columns[1] as AdaptiveColumn;
                Assert.IsNotNull(column);
                var content = column.Items[0] as AdaptiveTextBlock;
                Assert.IsNotNull(content);
                Assert.AreEqual(content.Text, MockData.MockShoppingItems[0].Topic);
                var speak = string.Format(MockData.AfterDeleteTaskMessage, MockData.MockShoppingItems[1].Topic, MockData.MockShoppingItems.Count - 1, MockData.Shopping);
                Assert.AreEqual(speak, responseCard.Speak);
            };
        }

        private Action<IActivity> AfterLastTaskDeletedMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var response = string.Format(MockData.AfterDeleteTaskMessage, MockData.MockTaskItems[0].Topic, 0, MockData.ToDo);
                Assert.AreEqual(response, messageActivity.Text);
            };
        }

        private string[] AfterDeletionRejectedMessage()
        {
            var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.ActionEnded);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] CollectTaskIndex()
        {
            var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.AskToDoTaskIndex);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] CollectConfirmationForToDo()
        {
            var response = ResponseManager.GetResponseTemplate(DeleteToDoResponses.AskDeletionConfirmation);
            return this.ParseReplies(response.Replies, new StringDictionary() { { MockData.ToDoTask, MockData.MockTaskItems[0].Topic } });
        }

        private string[] CollectConfirmationForShopping()
        {
            var response = ResponseManager.GetResponseTemplate(DeleteToDoResponses.AskDeletionConfirmation);
            return this.ParseReplies(response.Replies, new StringDictionary() { { MockData.ToDoTask, MockData.MockShoppingItems[1].Topic } });
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
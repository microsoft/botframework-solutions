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
using ToDoSkill.Dialogs.MarkToDo.Resources;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkillTest.Flow.Fakes;
using ToDoSkillTest.Flow.Utterances;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class MarkToDoFlowTests : ToDoBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            this.Services.LocaleConfigurations.Add(MockData.LocaleEN, new LocaleConfiguration()
            {
                Locale = MockData.LocaleENUS,
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>()
                {
                    { MockData.LuisGeneral, new MockLuisRecognizer(new GeneralTestUtterances()) },
                    { MockData.LuisToDo, new MockLuisRecognizer(new MarkToDoFlowTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_MarkToDoItem()
        {
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(MarkToDoFlowTestUtterances.BaseMarkTask)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectListType())
                .Send(MarkToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReplyOneOf(this.CollectTaskIndex())
                .Send(MarkToDoFlowTestUtterances.TaskContent)
                .AssertReply(this.ShowUpdatedToDoCard(0))
                .AssertReplyOneOf(this.CompleteAnotherTask())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_MarkToDoItem_By_Specific_Index()
        {
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(MarkToDoFlowTestUtterances.MarkSpecificTaskAsCompleted)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectListType())
                .Send(MarkToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedToDoCard(1))
                .AssertReplyOneOf(this.CompleteAnotherTask())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_MarkToDoItem_By_Specific_Index_And_ListType()
        {
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(MarkToDoFlowTestUtterances.MarkSpecificTaskAsCompletedWithListType)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedGroceryCard(2))
                .AssertReplyOneOf(this.CompleteAnotherTask())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_MarkToDoItem_By_Specific_Content()
        {
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(MarkToDoFlowTestUtterances.MarkTaskAsCompletedByContent)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectListType())
                .Send(MarkToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedToDoCard(0))
                .AssertReplyOneOf(this.CompleteAnotherTask())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> ShowUpdatedToDoCard(int index)
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
                    this.ParseReplies(ToDoSharedResponses.CardSummaryMessage.Replies, new StringDictionary() { { MockData.TaskCount, MockData.MockTaskItems.Count.ToString() }, { MockData.ListType, MockData.ToDo } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, MockData.PageSize);
                var columnSet = toDoChoices.Items[index] as AdaptiveColumnSet;
                Assert.IsNotNull(columnSet);
                var column = columnSet.Columns[0];
                Assert.IsNotNull(column);
                var image = column.Items[0] as AdaptiveImage;
                Assert.AreEqual(image.UrlString, IconImageSource.CheckIconSource);

                CollectionAssert.Contains(
                    this.ParseReplies(MarkToDoResponses.AfterTaskCompleted.Replies, new StringDictionary()
                    {
                        { MockData.TaskContent, MockData.MockTaskItems[index].Topic },
                        { MockData.ListType, MockData.ToDo }
                    }), responseCard.Speak);
            };
        }

        private Action<IActivity> ShowUpdatedGroceryCard(int index)
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
                    this.ParseReplies(ToDoSharedResponses.CardSummaryMessage.Replies, new StringDictionary() { { MockData.TaskCount, MockData.MockGroceryItems.Count.ToString() }, { MockData.ListType, MockData.Grocery } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, MockData.PageSize);
                var columnSet = toDoChoices.Items[index] as AdaptiveColumnSet;
                Assert.IsNotNull(columnSet);
                var column = columnSet.Columns[0];
                Assert.IsNotNull(column);
                var image = column.Items[0] as AdaptiveImage;
                Assert.AreEqual(image.UrlString, IconImageSource.CheckIconSource);

                CollectionAssert.Contains(
                    this.ParseReplies(MarkToDoResponses.AfterTaskCompleted.Replies, new StringDictionary()
                    {
                        { MockData.TaskContent, MockData.MockGroceryItems[index].Topic },
                        { MockData.ListType, MockData.Grocery }
                    }), responseCard.Speak);
            };
        }

        private string[] CollectListType()
        {
            return this.ParseReplies(MarkToDoResponses.ListTypePrompt.Replies, new StringDictionary());
        }

        private string[] CollectTaskIndex()
        {
            return this.ParseReplies(MarkToDoResponses.AskTaskIndex.Replies, new StringDictionary());
        }

        private string[] SettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.SettingUpOutlookMessage.Replies, new StringDictionary());
        }

        private string[] AfterSettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.AfterOutlookSetupMessage.Replies, new StringDictionary());
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Event);
            };
        }

        private string[] CompleteAnotherTask()
        {
            return this.ParseReplies(MarkToDoResponses.CompleteAnotherTaskPrompt.Replies, new StringDictionary());
        }

        private string[] ActionEndMessage()
        {
            return this.ParseReplies(ToDoSharedResponses.ActionEnded.Replies, new StringDictionary());
        }
    }
}
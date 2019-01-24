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
    public class DeleteAllToDosFlowTests : ToDoBotTestBase
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
                    { MockData.LuisToDo, new MockLuisRecognizer(new DeleteToDoFlowTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_DeleteAllToDoItems()
        {
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteAllTasks)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectListType())
                .Send(DeleteToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.CollectConfirmation())
                .Send(MockData.ConfirmYes)
                .AssertReply(this.ShowUpdatedCard())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteAllToDoItems_Confirm_No()
        {
            (this.ServiceManager as MockServiceManager).MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteAllTasks)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectListType())
                .Send(DeleteToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.CollectConfirmation())
                .Send(MockData.ConfirmNo)
                .AssertReply(this.ShowCardOfDeletionRefused())
                .StartTestAsync();
        }

        private string[] CollectListType()
        {
            return this.ParseReplies(DeleteToDoResponses.ListTypePrompt.Replies, new StringDictionary());
        }

        private Action<IActivity> CollectConfirmation()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(
                   this.ParseReplies(DeleteToDoResponses.AskDeletionAllConfirmation.Replies, new StringDictionary() { { MockData.ListType, MockData.ToDo } }),
                   messageActivity.Text);
            };
        }

        private Action<IActivity> ShowUpdatedCard()
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
                    this.ParseReplies(ToDoSharedResponses.CardSummaryMessage.Replies, new StringDictionary() { { MockData.TaskCount, "0" }, { MockData.ListType, MockData.ToDo } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, 0);

                CollectionAssert.Contains(
                  this.ParseReplies(DeleteToDoResponses.AfterAllTasksDeleted.Replies, new StringDictionary() { { MockData.ListType, MockData.ToDo } }),
                  responseCard.Speak);
            };
        }

        private Action<IActivity> ShowCardOfDeletionRefused()
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
                    this.ParseReplies(ToDoSharedResponses.CardSummaryMessage.Replies, new StringDictionary() { { MockData.TaskCount, MockData.MockTaskItems.Count.ToString() }, { MockData.ListType, MockData.ToDo } }),
                    adaptiveCardTitle.Text);
                Assert.AreEqual(toDoChoiceCount, MockData.PageSize);

                CollectionAssert.Contains(
                  this.ParseReplies(DeleteToDoResponses.DeletionAllConfirmationRefused.Replies, new StringDictionary() { { MockData.TaskCount, MockData.MockTaskItems.Count.ToString() }, { MockData.ListType, MockData.ToDo } }),
                  responseCard.Speak);
            };
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
    }
}
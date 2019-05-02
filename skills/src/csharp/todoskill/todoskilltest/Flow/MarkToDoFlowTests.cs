﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.MarkToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkillTest.Flow.Fakes;
using ToDoSkillTest.Flow.Utterances;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class MarkToDoFlowTests : ToDoBotTestBase
    {
        [TestMethod]
        public async Task Test_MarkToDoItem()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
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
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
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
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
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
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
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

                CollectionAssert.Contains(
                    this.ParseReplies(MarkToDoResponses.AfterTaskCompleted, new StringDictionary()
                    {
                        { MockData.TaskContent, MockData.MockTaskItems[index].Topic },
                        { MockData.ListType, MockData.ToDo }
                    }), messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowUpdatedGroceryCard(int index)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);

                CollectionAssert.Contains(
                    this.ParseReplies(MarkToDoResponses.AfterTaskCompleted, new StringDictionary()
                    {
                        { MockData.TaskContent, MockData.MockGroceryItems[index].Topic },
                        { MockData.ListType, MockData.Grocery }
                    }), messageActivity.Speak);
            };
        }

        private string[] CollectListType()
        {
            return this.ParseReplies(MarkToDoResponses.ListTypePromptForComplete, new StringDictionary());
        }

        private string[] CollectTaskIndex()
        {
            return this.ParseReplies(MarkToDoResponses.AskTaskIndexForComplete, new StringDictionary());
        }

        private string[] SettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.SettingUpOutlookMessage, new StringDictionary());
        }

        private string[] AfterSettingUpOneNote()
        {
            return this.ParseReplies(ToDoSharedResponses.AfterOutlookSetupMessage, new StringDictionary());
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var message = activity.AsMessageActivity();
                Assert.AreEqual(1, message.Attachments.Count);
                Assert.AreEqual("application/vnd.microsoft.card.oauth", message.Attachments[0].ContentType);
            };
        }

        private string[] CompleteAnotherTask()
        {
            return this.ParseReplies(MarkToDoResponses.CompleteAnotherTaskPrompt, new StringDictionary());
        }

        private string[] ActionEndMessage()
        {
            return this.ParseReplies(ToDoSharedResponses.ActionEnded, new StringDictionary());
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.DeleteToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkillTest.Flow.Fakes;
using ToDoSkillTest.Flow.Utterances;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class DeleteToDoFlowTests : ToDoBotTestBase
    {
        [TestMethod]
        public async Task Test_DeleteToDoItem()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.BaseDeleteTask)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectListType())
                .Send(DeleteToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReplyOneOf(this.CollectTaskIndex())
                .Send(DeleteToDoFlowTestUtterances.TaskContent)
                .AssertReply(this.ShowUpdatedToDoCard())
                .AssertReplyOneOf(this.DeleteAnotherTask())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem_By_Specific_Index()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteSpecificTask)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectListType())
                .Send(DeleteToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedToDoCard())
                .AssertReplyOneOf(this.DeleteAnotherTask())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem_By_Specific_Index_And_ListType()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteSpecificTaskWithListType)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedShoppingCard())
                .AssertReplyOneOf(this.DeleteAnotherTask())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DeleteToDoItem_By_Specific_Content()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteTaskByContent)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.CollectListType())
                .Send(DeleteToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedToDoCard())
                .AssertReplyOneOf(this.DeleteAnotherTask())
                .Send(MockData.ConfirmNo)
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> ShowUpdatedToDoCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);

                CollectionAssert.Contains(
                    this.ParseReplies(DeleteToDoResponses.AfterTaskDeleted, new StringDictionary()
                    {
                        { MockData.TaskContent, MockData.MockTaskItems[0].Topic },
                        { MockData.ListType, MockData.ToDo }
                    }), messageActivity.Speak);
            };
        }

        private Action<IActivity> ShowUpdatedShoppingCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);

                CollectionAssert.Contains(
                    this.ParseReplies(DeleteToDoResponses.AfterTaskDeleted, new StringDictionary()
                    {
                        { MockData.TaskContent, MockData.MockShoppingItems[1].Topic },
                        { MockData.ListType, MockData.Shopping }
                    }), messageActivity.Speak);
            };
        }

        private string[] CollectTaskIndex()
        {
            return this.ParseReplies(DeleteToDoResponses.AskTaskIndexForDelete, new StringDictionary());
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

        private string[] DeleteAnotherTask()
        {
            return this.ParseReplies(DeleteToDoResponses.DeleteAnotherTaskPrompt, new StringDictionary());
        }

        private string[] ActionEndMessage()
        {
            return this.ParseReplies(ToDoSharedResponses.ActionEnded, new StringDictionary());
        }

        private string[] CollectListType()
        {
            return this.ParseReplies(DeleteToDoResponses.ListTypePromptForDelete, new StringDictionary());
        }
    }
}
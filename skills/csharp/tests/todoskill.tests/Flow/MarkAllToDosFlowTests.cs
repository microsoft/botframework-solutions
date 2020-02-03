// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.Main;
using ToDoSkill.Responses.MarkToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Tests.Flow.Fakes;
using ToDoSkill.Tests.Flow.Utterances;

namespace ToDoSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class MarkAllToDosFlowTests : ToDoSkillTestBase
    {
        [TestMethod]
        public async Task Test_MarkAllToDoItems()
        {
            ServiceManager.MockTaskService.ChangeData(DataOperationType.OperationType.ResetAllData);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.ToDoWelcomeMessage))
                .Send(MarkToDoFlowTestUtterances.MarkAllTasksAsCompleted)
                .AssertReplyOneOf(this.CollectListType())
                .Send(MarkToDoFlowTestUtterances.ConfirmListType)
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.ShowUpdatedCard())
                .StartTestAsync();
        }

        private Action<IActivity> ShowUpdatedCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(
                 this.AfterAllTasksCompleted(new
                 {
                     ListType = MockData.ToDo
                 }), messageActivity.Speak);
            };
        }

        private string[] AfterAllTasksCompleted(object data)
        {
            return GetTemplates(MarkToDoResponses.AfterAllTasksCompleted, data);
        }

        private string[] SettingUpOneNote()
        {
            return GetTemplates(ToDoSharedResponses.SettingUpOutlookMessage);
        }

        private string[] AfterSettingUpOneNote()
        {
            return GetTemplates(ToDoSharedResponses.AfterOutlookSetupMessage);
        }

        private string[] CollectListType()
        {
            return GetTemplates(MarkToDoResponses.ListTypePromptForComplete);
        }
    }
}
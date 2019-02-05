// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
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
                    { MockData.LuisToDo, new MockLuisRecognizer(new DeleteToDoFlowTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_DeleteAllToDoItems()
        {
            await this.GetTestFlow()
                .Send(DeleteToDoFlowTestUtterances.DeleteAllTasks)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.CollectConfirmation())
                .Send("yes")
                .AssertReply(this.AfterAllTasksDeletedTextMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> CollectConfirmation()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var response = ResponseManager.GetResponseTemplate(DeleteToDoResponses.AskDeletionAllConfirmation);
                CollectionAssert.Contains(
                   this.ParseReplies(response.Replies, new StringDictionary() { { MockData.ListType, MockData.ToDo } }),
                   messageActivity.Text);
            };
        }

        private Action<IActivity> AfterAllTasksDeletedTextMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var response = ResponseManager.GetResponseTemplate(DeleteToDoResponses.AfterAllTasksDeleted);
                CollectionAssert.Contains(
                   this.ParseReplies(response.Replies, new StringDictionary() { { MockData.ListType, MockData.ToDo } }),
                   messageActivity.Text);
            };
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
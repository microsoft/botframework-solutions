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
    public class MarkAllToDosFlowTests : ToDoBotTestBase
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
                    { MockData.LuisToDo, new MockLuisRecognizer(new MarkToDoFlowTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_MarkAllToDoItems()
        {
            await this.GetTestFlow()
                .Send(MarkToDoFlowTestUtterances.MarkAllTasksAsCompleted)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.SettingUpOneNote())
                .AssertReplyOneOf(this.AfterSettingUpOneNote())
                .AssertReply(this.AfterAllTasksMarkedCardMessage())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> AfterAllTasksMarkedCardMessage()
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
                foreach (var choice in toDoChoices.Items)
                {
                    var columnSet = choice as AdaptiveColumnSet;
                    Assert.IsNotNull(columnSet);
                    var column = columnSet.Columns[0];
                    Assert.IsNotNull(column);
                    var image = column.Items[0] as AdaptiveImage;
                    Assert.IsNotNull(image);
                    Assert.AreEqual(image.UrlString, IconImageSource.CheckIconSource);
                }
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
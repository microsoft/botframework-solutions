// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Responses.Main;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Services;
using CalendarSkill.Test.Flow.Fakes;
using CalendarSkill.Test.Flow.Utterances;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkill.Test.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class UpdateCalendarFlowTests : CalendarSkillTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var botServices = Services.BuildServiceProvider().GetService<BotServices>();
            botServices.CognitiveModelSets.Add("en-us", new CognitiveModelSet()
            {
                LuisServices = new Dictionary<string, LuisRecognizer>()
                {
                    { "General", new MockLuisRecognizer() },
                    { "Calendar", new MockLuisRecognizer(new UpdateMeetingTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarUpdateWithNewStartDateEntity()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.FirstPromptMessage))
                .Send(UpdateMeetingTestUtterances.UpdateMeetingWithNewStartDate)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.UpdateEventPrompt())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarUpdateWithTitleEntity()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.FirstPromptMessage))
                .Send(UpdateMeetingTestUtterances.UpdateMeetingWithTitle)
                .AssertReplyOneOf(this.AskForNewTimePrompt())
                .Send("tomorrow 9 pm")
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.UpdateEventPrompt())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarUpdateWithMoveEarlierTimeSpan()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.FirstPromptMessage))
                .Send(UpdateMeetingTestUtterances.UpdateMeetingWithMoveEarlierTimeSpan)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.UpdateEventPrompt())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarUpdateWithMoveLaterTimeSpan()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.FirstPromptMessage))
                .Send(UpdateMeetingTestUtterances.UpdateMeetingWithMoveLaterTimeSpan)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.UpdateEventPrompt())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarUpdateFromMultipleEvents()
        {
            int eventCount = 3;
            this.ServiceManager = MockServiceManager.SetMeetingsToMultiple(eventCount);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.FirstPromptMessage))
                .Send(UpdateMeetingTestUtterances.UpdateMeetingWithStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(GeneralTestUtterances.ChooseOne)
                .AssertReplyOneOf(this.AskForNewTimePrompt())
                .Send("tomorrow 9 pm")
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.UpdateEventPrompt())
                .StartTestAsync();
        }

        private string[] AskForNewTimePrompt()
        {
            return GetTemplates(UpdateEventResponses.NoNewTime);
        }

        private Action<IActivity> ShowCalendarList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private string[] UpdateEventPrompt()
        {
            return GetTemplates(UpdateEventResponses.EventUpdated);
        }
    }
}
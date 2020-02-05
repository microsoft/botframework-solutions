// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Responses.ChangeEventStatus;
using CalendarSkill.Responses.Main;
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
    public class ChangeCalendarStatusFlowTests : CalendarSkillTestBase
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
                    { "Calendar", new MockLuisRecognizer(new ChangeMeetingStatusTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarDeleteWithStartTimeEntity()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(ChangeMeetingStatusTestUtterances.DeleteMeetingWithStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarDeleteWithTitleEntity()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(ChangeMeetingStatusTestUtterances.DeleteMeetingWithTitle)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarDeleteFromMultipleEvents()
        {
            int eventCount = 3;
            this.ServiceManager = MockServiceManager.SetMeetingsToMultiple(eventCount);
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(ChangeMeetingStatusTestUtterances.DeleteMeetingWithTitle)
                .AssertReply(this.ShowCalendarList())
                .Send(GeneralTestUtterances.ChooseOne)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarAcceptWithStartTimeEntity()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(ChangeMeetingStatusTestUtterances.AcceptMeetingWithStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.AcceptEventPrompt())
                .StartTestAsync();
        }

        private string[] DeleteEventPrompt()
        {
            return GetTemplates(ChangeEventStatusResponses.EventDeleted);
        }

        private string[] AcceptEventPrompt()
        {
            return GetTemplates(ChangeEventStatusResponses.EventAccepted);
        }

        private Action<IActivity> ShowCalendarList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
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
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.JoinEvent;
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
    public class ConnectToMeetingFlowTests : CalendarSkillTestBase
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
                    { "Calendar", new MockLuisRecognizer(new ConnectToMeetingUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarJoinNumberWithStartTimeEntity()
        {
            var now = DateTime.Now;
            var startTime = new DateTime(now.Year, now.Month, now.Day, 18, 0, 0);
            startTime = startTime.AddDays(1);
            startTime = TimeZoneInfo.ConvertTimeToUtc(startTime);
            this.ServiceManager = MockServiceManager.SetMeetingsToSpecial(new List<EventModel>()
            {
                MockCalendarService.CreateEventModel(
                    startDateTime: startTime,
                    endDateTime: startTime.AddHours(1),
                    content: "<a href=\"tel:12345678 \">12345678</a>")
            });
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.FirstPromptMessage))
                .Send(ConnectToMeetingUtterances.JoinMeetingWithStartTime)
                .AssertReplyOneOf(this.ConfirmPhoneNumberPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.JoinMeetingResponse())
                .AssertReply(this.JoinMeetingEvent())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarJoinLinkWithStartTimeEntity()
        {
            var now = DateTime.Now;
            var startTime = new DateTime(now.Year, now.Month, now.Day, 18, 0, 0);
            startTime = startTime.AddDays(1);
            startTime = TimeZoneInfo.ConvertTimeToUtc(startTime);
            this.ServiceManager = MockServiceManager.SetMeetingsToSpecial(new List<EventModel>()
            {
                MockCalendarService.CreateEventModel(
                    startDateTime: startTime,
                    endDateTime: startTime.AddHours(1),
                    content: "<a href=\"meetinglink\">Join Microsoft Teams Meeting</a>")
            });
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.FirstPromptMessage))
                .Send(ConnectToMeetingUtterances.JoinMeetingWithStartTime)
                .AssertReplyOneOf(this.ConfirmMeetingLinkPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.JoinMeetingResponse())
                .AssertReply(this.JoinMeetingEvent())
                .StartTestAsync();
        }

        private string[] ConfirmPhoneNumberPrompt()
        {
            return GetTemplates(JoinEventResponses.ConfirmPhoneNumber, new
            {
                PhoneNumber = "12345678"
            });
        }

        private string[] ConfirmMeetingLinkPrompt()
        {
            return GetTemplates(JoinEventResponses.ConfirmMeetingLink, new
            {
                MeetingLink = "meetinglink"
            });
        }

        private string[] JoinMeetingResponse()
        {
            return GetTemplates(JoinEventResponses.JoinMeeting);
        }

        private Action<IActivity> JoinMeetingEvent()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Event);
            };
        }
    }
}
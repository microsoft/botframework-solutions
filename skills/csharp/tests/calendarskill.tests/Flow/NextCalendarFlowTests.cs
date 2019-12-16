﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Services;
using CalendarSkill.Test.Flow.Fakes;
using CalendarSkill.Test.Flow.Utterances;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkill.Test.Flow
{
    [TestClass]
    public class NextCalendarFlowTests : CalendarSkillTestBase
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
                    { "Calendar", new MockLuisRecognizer(new FindMeetingTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarOneNextMeeting()
        {
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseNextMeeting)
                .AssertReplyOneOf(this.NextMeetingPrompt())
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarOneNextMeeting_AskHowLong()
        {
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.HowLongNextMeetingMeeting)
                .AssertReplyOneOf(this.BeforeShowEventDetailsPrompt())
                .AssertReplyOneOf(this.ReadDurationPrompt())
                .AssertReplyOneOf(this.NextMeetingPrompt())
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarOneNextMeeting_AskWhere()
        {
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.WhereNextMeetingMeeting)
                .AssertReplyOneOf(this.BeforeShowEventDetailsPrompt())
                .AssertReplyOneOf(this.ReadLocationPrompt())
                .AssertReplyOneOf(this.NextMeetingPrompt())
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarOneNextMeeting_AskWhen()
        {
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.WhenNextMeetingMeeting)
                .AssertReplyOneOf(this.BeforeShowEventDetailsPrompt())
                .AssertReplyOneOf(this.ReadTimePrompt())
                .AssertReplyOneOf(this.NextMeetingPrompt())
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarNoNextMeetings()
        {
            this.ServiceManager = MockServiceManager.SetMeetingsToNull();
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseNextMeeting)
                .AssertReplyOneOf(this.NoMeetingResponse())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarMultipleMeetings()
        {
            int eventCount = 3;
            this.ServiceManager = MockServiceManager.SetMeetingsToMultiple(eventCount);
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseNextMeeting)
                .AssertReplyOneOf(this.NextMeetingPrompt())
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] NextMeetingPrompt()
        {
            return GetTemplates(SummaryResponses.ShowNextMeetingMessage);
        }

        private string[] BeforeShowEventDetailsPrompt()
        {
            return GetTemplates(SummaryResponses.BeforeShowEventDetails, new
            {
                EventName = Strings.Strings.DefaultEventName
            });
        }

        private string[] ReadTimePrompt()
        {
            return GetTemplates(SummaryResponses.ReadTime, new
            {
                EventStartTime = "6:00 PM",
                EventEndTime = "7:00 PM"
            });
        }

        private string[] ReadDurationPrompt()
        {
            return GetTemplates(SummaryResponses.ReadDuration, new
            {
                EventDuration = Strings.Strings.DefaultDuration
            });
        }

        private string[] ReadLocationPrompt()
        {
            return GetTemplates(SummaryResponses.ReadLocation, new
            {
                EventLocation = Strings.Strings.DefaultLocation
            });
        }

        private Action<IActivity> ShowCalendarList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private string[] NoMeetingResponse()
        {
            return GetTemplates(SummaryResponses.ShowNoMeetingMessage);
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Handoff);
            };
        }
    }
}
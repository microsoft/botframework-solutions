﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Services;
using CalendarSkillTest.Flow.Fakes;
using CalendarSkillTest.Flow.Utterances;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class NextCalendarFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var botServices = Services.BuildServiceProvider().GetService<BotServices>();
            botServices.CognitiveModelSets.Add("en", new CognitiveModelSet()
            {
                LuisServices = new Dictionary<string, ITelemetryRecognizer>()
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
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.NextMeetingPrompt())
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] NextMeetingPrompt()
        {
            return this.ParseReplies(SummaryResponses.ShowNextMeetingMessage, new StringDictionary());
        }

        private string[] BeforeShowEventDetailsPrompt()
        {
            var responseParams = new StringDictionary()
            {
                { "EventName", Strings.Strings.DefaultEventName },
            };
            return this.ParseReplies(SummaryResponses.BeforeShowEventDetails, responseParams);
        }

        private string[] ReadTimePrompt()
        {
            var responseParams = new StringDictionary()
            {
                { "EventStartTime", "6:00 PM" },
                { "EventEndTime", "7:00 PM" },
            };
            return this.ParseReplies(SummaryResponses.ReadTime, responseParams);
        }

        private string[] ReadDurationPrompt()
        {
            var responseParams = new StringDictionary()
            {
                { "EventDuration", Strings.Strings.DefaultDuration },
            };
            return this.ParseReplies(SummaryResponses.ReadDuration, responseParams);
        }

        private string[] ReadLocationPrompt()
        {
            var responseParams = new StringDictionary()
            {
                { "EventLocation", Strings.Strings.DefaultLocation },
            };
            return this.ParseReplies(SummaryResponses.ReadLocation, responseParams);
        }

        private string[] ReadNoLocationPrompt()
        {
            return this.ParseReplies(SummaryResponses.ReadNoLocation, new StringDictionary());
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
            };
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
            return this.ParseReplies(SummaryResponses.ShowNoMeetingMessage, new StringDictionary());
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
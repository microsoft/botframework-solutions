﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Services;
using CalendarSkillTest.Flow.Fakes;
using CalendarSkillTest.Flow.Utterances;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class UpdateCalendarFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var botServices = Services.BuildServiceProvider().GetService<BotServices>();
            botServices.CognitiveModelSets.Add("en", new CognitiveModelSet()
            {
                LuisServices = new Dictionary<string, IRecognizer>()
                {
                    { "general", new MockLuisRecognizer() },
                    { "calendar", new MockLuisRecognizer(new UpdateMeetingTestUtterances()) }
                }
            });

            // keep this use old mock, Moq has some conflict with Prompt. It will throw exception in GetEventPrompt
            this.ServiceManager = new MockCalendarServiceManager();
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(MockCalendarService.FakeDefaultEvents());
            serviceManager.SetupUserService(MockUserService.FakeDefaultUsers(), MockUserService.FakeDefaultPeople());
        }

        // TODO: These tests caused some issue with the bot state. Needs to be refactored.
        // [TestMethod]
        // public async Task Test_CalendarUpdateByTitle()
        // {
        //     await this.GetTestFlow()
        //         .Send(UpdateMeetingTestUtterances.BaseUpdateMeeting)
        //         .AssertReply(this.ShowAuth())
        //         .Send(this.GetAuthResponse())
        //         .AssertReplyOneOf(this.AskForTitleTimePrompt())
        //         .Send(Strings.Strings.DefaultEventName)
        //         .AssertReplyOneOf(this.AskForNewTimePrompt())
        //         .Send("tomorrow 9 PM")
        //         .AssertReply(this.ShowCalendarList())
        //         .Send(Strings.Strings.ConfirmYes)
        //         .AssertReply(this.ShowCalendarList())
        //         .AssertReply(this.ActionEndMessage())
        //         .StartTestAsync();
        // }

        // [TestMethod]
        // public async Task Test_CalendarUpdateByStartTime()
        // {
        //     var now = DateTime.Now;
        //     var startTime = new DateTime(now.Year, now.Month, now.Day, 18, 0, 0);
        //     startTime = startTime.AddDays(1);
        //     startTime = TimeZoneInfo.ConvertTimeToUtc(startTime);
        //     var serviceManager = this.ServiceManager as MockCalendarServiceManager;
        //     serviceManager.SetupCalendarService(new List<EventModel>
        //     {
        //         MockCalendarService.CreateEventModel(
        //             startDateTime: startTime,
        //             endDateTime: startTime.AddHours(1))
        //     });
        //     await this.GetTestFlow()
        //         .Send(UpdateMeetingTestUtterances.BaseUpdateMeeting)
        //         .AssertReply(this.ShowAuth())
        //         .Send(this.GetAuthResponse())
        //         .AssertReplyOneOf(this.AskForTitleTimePrompt())
        //         .Send("tomorrow 6 pm")
        //         .AssertReplyOneOf(this.AskForNewTimePrompt())
        //         .Send("tomorrow 9 pm")
        //         .AssertReply(this.ShowCalendarList())
        //         .Send(Strings.Strings.ConfirmYes)
        //         .AssertReply(this.ShowCalendarList())
        //         .AssertReply(this.ActionEndMessage())
        //         .StartTestAsync();
        // }
        [TestMethod]
        public async Task Test_CalendarUpdateWithStartTimeEntity()
        {
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            var now = DateTime.Now;
            var startTime = new DateTime(now.Year, now.Month, now.Day, 18, 0, 0);
            startTime = startTime.AddDays(1);
            startTime = TimeZoneInfo.ConvertTimeToUtc(startTime);
            serviceManager.SetupCalendarService(new List<EventModel>()
            {
                MockCalendarService.CreateEventModel(
                    startDateTime: startTime,
                    endDateTime: startTime.AddHours(1))
            });
            await this.GetTestFlow()
                .Send(UpdateMeetingTestUtterances.UpdateMeetingWithStartTime)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForNewTimePrompt())
                .Send("tomorrow 9 pm")
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarUpdateWithTitleEntity()
        {
            await this.GetTestFlow()
                .Send(UpdateMeetingTestUtterances.UpdateMeetingWithTitle)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForNewTimePrompt())
                .Send("tomorrow 9 pm")
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] AskForTitleTimePrompt()
        {
            return this.ParseReplies(UpdateEventResponses.NoUpdateStartTime, new StringDictionary());
        }

        private string[] AskForNewTimePrompt()
        {
            return this.ParseReplies(UpdateEventResponses.NoNewTime, new StringDictionary());
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

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }
    }
}
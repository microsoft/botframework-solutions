﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.ChangeEventStatus;
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
    public class DeleteCalendarFlowTests : CalendarBotTestBase
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
                    { "Calendar", new MockLuisRecognizer(new DeleteMeetingTestUtterances()) }
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
        // public async Task Test_CalendarDeleteByTitle()
        // {
        //     await this.GetTestFlow()
        //         .Send(DeleteMeetingTestUtterances.BaseDeleteMeeting)
        //         .AssertReply(this.ShowAuth())
        //         .Send(this.GetAuthResponse())
        //         .AssertReplyOneOf(this.AskForDeletePrompt())
        //         .Send(Strings.Strings.DefaultEventName)
        //         .AssertReply(this.ShowCalendarList())
        //         .Send(Strings.Strings.ConfirmYes)
        //         .AssertReplyOneOf(this.DeleteEventPrompt())
        //         .AssertReply(this.ActionEndMessage())
        //         .StartTestAsync();
        // }

        // [TestMethod]
        // public async Task Test_CalendarDeleteByStartTime()
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
        //         .Send(DeleteMeetingTestUtterances.BaseDeleteMeeting)
        //         .AssertReply(this.ShowAuth())
        //         .Send(this.GetAuthResponse())
        //         .AssertReplyOneOf(this.AskForDeletePrompt())
        //         .Send("tomorrow 6 pm")
        //         .AssertReply(this.ShowCalendarList())
        //         .Send(Strings.Strings.ConfirmYes)
        //         .AssertReplyOneOf(this.DeleteEventPrompt())
        //         .AssertReply(this.ActionEndMessage())
        //         .StartTestAsync();
        // }
        [TestMethod]
        public async Task Test_CalendarDeleteWithStartTimeEntity()
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
                .Send(DeleteMeetingTestUtterances.DeleteMeetingWithStartTime)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarDeleteWithTitleEntity()
        {
            await this.GetTestFlow()
                .Send(DeleteMeetingTestUtterances.DeleteMeetingWithTitle)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] AskForDeletePrompt()
        {
            return this.ParseReplies(ChangeEventStatusResponses.NoDeleteStartTime, new StringDictionary());
        }

        private string[] DeleteEventPrompt()
        {
            return this.ParseReplies(ChangeEventStatusResponses.EventDeleted, new StringDictionary());
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
                Assert.AreEqual(activity.Type, ActivityTypes.Handoff);
            };
        }
    }
}
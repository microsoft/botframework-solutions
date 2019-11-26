﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Responses.ChangeEventStatus;
using CalendarSkill.Responses.CheckAvailable;
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
    public class CheckAvailableFlowTests : CalendarSkillTestBase
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
                    { "Calendar", new MockLuisRecognizer(new CheckAvailableTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarBaseCheckAvailable()
        {
            await this.GetTestFlow()
                .Send(CheckAvailableTestUtterances.BaseCheckAvailable)
                .AssertReplyOneOf(this.AvailableResponse())
                .AssertReplyOneOf(this.AskForCreateNewMeeting())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCheckAvailableSlotFilling()
        {
            this.ServiceManager = MockServiceManager.SetAllToDefault();
            await this.GetTestFlow()
                .Send(CheckAvailableTestUtterances.CheckAvailableSlotFilling)
                .AssertReplyOneOf(this.AskForCollectContact())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(this.AskForCollectTime())
                .Send("4 pm")
                .AssertReplyOneOf(this.AvailableResponse())
                .AssertReplyOneOf(this.AskForCreateNewMeeting())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCheckAvailableNotAvailable()
        {
            this.ServiceManager = MockServiceManager.SetParticipantNotAvailable();
            await this.GetTestFlow()
                .Send(CheckAvailableTestUtterances.BaseCheckAvailable)
                .AssertReplyOneOf(this.NotAvailableResponse())
                .AssertReplyOneOf(this.AskForFindNextAvailableTimeResponse())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.BothAvailableResponse())
                .AssertReplyOneOf(this.AskForCreateNewMeeting())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCheckAvailableOrgnizerNotAvailable()
        {
            this.ServiceManager = MockServiceManager.SetOrgnizerNotAvailable();
            await this.GetTestFlow()
                .Send(CheckAvailableTestUtterances.BaseCheckAvailable)
                .AssertReplyOneOf(this.OrgnizerNotAvailableResponse())
                .AssertReplyOneOf(this.AskForCreateNewMeetingAnyway())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] AvailableResponse()
        {
            return this.ParseReplies(CheckAvailableResponses.AttendeeIsAvailable, new StringDictionary()
            {
                { "UserName", Strings.Strings.DefaultUserName },
                { "StartTime", "4:00 PM" },
                { "EndTime", "5:00 PM" },
                { "Date", "today" },
            });
        }

        private string[] BothAvailableResponse()
        {
            return this.ParseReplies(CheckAvailableResponses.NextBothAvailableTime, new StringDictionary()
            {
                { "UserName", Strings.Strings.DefaultUserName },
                { "StartTime", "4:30 PM" },
                { "EndTime", "5:00 PM" },
                { "EndDate", "today" },
            });
        }

        private string[] NotAvailableResponse()
        {
            return this.ParseReplies(CheckAvailableResponses.NotAvailable, new StringDictionary()
            {
                { "UserName", Strings.Strings.DefaultUserName },
                { "Time", "4:00 PM" },
                { "Date", "today" },
            });
        }

        private string[] OrgnizerNotAvailableResponse()
        {
            return this.ParseReplies(CheckAvailableResponses.AttendeeIsAvailableOrgnizerIsUnavailableWithOneConflict, new StringDictionary()
            {
                { "UserName", Strings.Strings.DefaultUserName },
                { "StartTime", "4:00 PM" },
                { "EndTime", "5:00 PM" },
                { "Title", Strings.Strings.DefaultEventName },
                { "EventStartTime", "4:00 PM" },
                { "EventEndTime", "5:00 PM" },
                { "Date", "today" },
            });
        }

        private string[] AskForFindNextAvailableTimeResponse()
        {
            return this.ParseReplies(CheckAvailableResponses.AskForNextAvailableTime, new StringDictionary()
            {
                { "UserName", Strings.Strings.DefaultUserName }
            });
        }

        private string[] AskForCreateNewMeeting()
        {
            return this.ParseReplies(CheckAvailableResponses.AskForCreateNewMeeting, new StringDictionary());
        }

        private string[] AskForCreateNewMeetingAnyway()
        {
            return this.ParseReplies(CheckAvailableResponses.AskForCreateNewMeetingAnyway, new StringDictionary()
            {
                { "UserName", Strings.Strings.DefaultUserName },
                { "StartTime", "4:00 PM" },
            });
        }

        private string[] AskForCollectContact()
        {
            return this.ParseReplies(CheckAvailableResponses.AskForCheckAvailableUserName, new StringDictionary());
        }

        private string[] AskForCollectTime()
        {
            return this.ParseReplies(CheckAvailableResponses.AskForCheckAvailableTime, new StringDictionary());
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
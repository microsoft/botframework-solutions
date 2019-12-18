﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.TimeRemaining;
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
    public class TimeRemainingFlowTests : CalendarSkillTestBase
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
                    { "Calendar", new MockLuisRecognizer(new TimeRemainingUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarNextMeetingTimeRemaining()
        {
            this.ServiceManager = MockServiceManager.SetMeetingsToSpecial(new List<EventModel>() { MockServiceManager.CreateEventModel(startDateTime: DateTime.UtcNow.AddDays(1)) });
            await this.GetTestFlow()
                .Send(TimeRemainingUtterances.NextMeetingTimeRemaining)
                .AssertReplyOneOf(this.ShowNextMeetingRemainingTime())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] ShowNextMeetingRemainingTime()
        {
            return GetTemplates(TimeRemainingResponses.ShowNextMeetingTimeRemainingMessage, new
            {
                RemainingTime = "23 hours 59 minutes "
            });
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
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.TimeRemaining;
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
    public class TimeRemainingFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var botServices = Services.BuildServiceProvider().GetService<BotServices>();
            botServices.CognitiveModelSets.Add("en", new CognitiveModelSet()
            {
                LuisServices = new Dictionary<string, ITelemetryRecognizer>()
                {
                    { "general", new MockLuisRecognizer() },
                    { "calendar", new MockLuisRecognizer(new TimeRemainingUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarNextMeetingTimeRemaining()
        {
            this.ServiceManager = MockServiceManager.SetMeetingsToSpecial(new List<EventModel>() { MockServiceManager.CreateEventModel(startDateTime: DateTime.UtcNow.AddDays(1)) });
            await this.GetTestFlow()
                .Send(TimeRemainingUtterances.NextMeetingTimeRemaining)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ShowNextMeetingRemainingTime())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
            };
        }

        private string[] ShowNextMeetingRemainingTime()
        {
            var responseParams = new StringDictionary()
            {
                { "RemainingTime", "23 hours 59 minutes " },
            };

            return this.ParseReplies(TimeRemainingResponses.ShowNextMeetingTimeRemainingMessage, responseParams);
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
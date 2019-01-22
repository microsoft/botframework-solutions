using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.TimeRemain.Resources;
using CalendarSkill.Models;
using CalendarSkillTest.Flow.Fakes;
using CalendarSkillTest.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class TimeRemainingFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            this.Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>()
                {
                    { "general", new MockLuisRecognizer() },
                    { "calendar", new MockLuisRecognizer(new TimeRemainingUtterances()) }
                }
            });

            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(MockCalendarService.FakeDefaultEvents());
            serviceManager.SetupUserService(MockUserService.FakeDefaultUsers(), MockUserService.FakeDefaultPeople());
        }

        [TestMethod]
        public async Task Test_CalendarNextMeetingTimeRemaining()
        {
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(new List<EventModel>() { MockCalendarService.CreateEventModel(startDateTime: DateTime.UtcNow.AddDays(1)) });
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

            return this.ParseReplies(TimeRemainResponses.ShowNextMeetingTimeRemainingMessage.Replies, responseParams);
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
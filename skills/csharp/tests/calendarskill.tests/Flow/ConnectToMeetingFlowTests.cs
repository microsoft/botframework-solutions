using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.JoinEvent;
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
    public class ConnectToMeetingFlowTests : CalendarSkillTestBase
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
                .Send(ConnectToMeetingUtterances.JoinMeetingWithStartTime)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmPhoneNumberPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.JoinMeetingResponse())
                .AssertReply(this.JoinMeetingEvent())
                .AssertReply(this.ActionEndMessage())
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
                .Send(ConnectToMeetingUtterances.JoinMeetingWithStartTime)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmMeetingLinkPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.JoinMeetingResponse())
                .AssertReply(this.JoinMeetingEvent())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] ConfirmPhoneNumberPrompt()
        {
            return this.ParseReplies(JoinEventResponses.ConfirmPhoneNumber, new StringDictionary() { { "PhoneNumber", "12345678" } });
        }

        private string[] ConfirmMeetingLinkPrompt()
        {
            return this.ParseReplies(JoinEventResponses.ConfirmMeetingLink, new StringDictionary() { { "MeetingLink", "meetinglink" } });
        }

        private string[] JoinMeetingResponse()
        {
            return this.ParseReplies(JoinEventResponses.JoinMeeting, new StringDictionary());
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
            };
        }

        private Action<IActivity> JoinMeetingEvent()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Event);
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
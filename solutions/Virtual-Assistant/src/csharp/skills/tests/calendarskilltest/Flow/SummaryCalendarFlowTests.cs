using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.Summary.Resources;
using CalendarSkillTest.Flow.Fakes;
using CalendarSkillTest.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class SummaryCalendarFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupUserService(MockUserService.FakeDefaultUsers(), MockUserService.FakeDefaultPeople());

            this.Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>()
                {
                    { "general", new MockLuisRecognizer() },
                    { "calendar", new MockLuisRecognizer(new FindMeetingTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarSummary()
        {
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(MockCalendarService.FakeDefaultEvents());
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseFindMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.FoundEventPrompt())
                .AssertReply(this.ShowCalendarList())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send("No")
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSummaryByTimeRange()
        {
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(new List<EventModel>() { MockCalendarService.CreateEventModel(startDateTime: DateTime.UtcNow.AddDays(7), endDateTime: DateTime.UtcNow.AddDays(8)) });
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseFindMeetingByTimeRange)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.FoundEventPrompt())
                .AssertReply(this.ShowCalendarList())
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send("No")
                .AssertReplyOneOf(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] ActionEndMessage()
        {
            return this.ParseReplies(CalendarSharedResponses.CancellingMessage.Replies, new StringDictionary());
        }

        private string[] WelcomePrompt()
        {
            return this.ParseReplies(CalendarMainResponses.CalendarWelcomeMessage.Replies, new StringDictionary());
        }

        private string[] FoundEventPrompt()
        {
            var responseParams = new StringDictionary()
            {
                { "Count", "1" },
                { "EventName1", "test title" },
                { "EventDuration", "1 hour" },
            };

            return this.ParseReplies(SummaryResponses.ShowOneMeetingSummaryMessage.Replies, responseParams);
        }

        private Action<IActivity> ShowCalendarList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
            };
        }

        private string[] ReadOutMorePrompt()
        {
            return this.ParseReplies(SummaryResponses.ReadOutMorePrompt.Replies, new StringDictionary());
        }
    }
}

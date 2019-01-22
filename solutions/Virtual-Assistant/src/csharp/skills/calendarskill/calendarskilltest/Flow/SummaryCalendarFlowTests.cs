using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Summary.Resources;
using CalendarSkill.Models;
using CalendarSkillTest.Flow.Fakes;
using CalendarSkillTest.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Resources;
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
                .AssertReplyOneOf(this.FoundOneEventPrompt())
                .AssertReply(this.ShowCalendarList(1))
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarNoEventSummary()
        {
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(new List<EventModel>());
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseFindMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.NoEventResponse())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSummaryGetMultipleMeetings()
        {
            int eventCount = 3;
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(MockCalendarService.FakeMultipleEvents(eventCount));
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseFindMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.FoundMultipleEventPrompt(eventCount))
                .AssertReply(this.ShowCalendarList(eventCount))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSummaryReadOutWithOneMeeting()
        {
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(MockCalendarService.FakeDefaultEvents());
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseFindMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.FoundOneEventPrompt())
                .AssertReply(this.ShowCalendarList(1))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowReadOutEventList())
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSummaryReadOutWithMutipleMeeting()
        {
            int eventCount = 3;
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(MockCalendarService.FakeMultipleEvents(eventCount));
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseFindMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.FoundMultipleEventPrompt(eventCount))
                .AssertReply(this.ShowCalendarList(eventCount))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(FindMeetingTestUtterances.ChooseFirstMeeting)
                .AssertReply(this.ShowReadOutEventList())
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSummaryByTimeRange()
        {
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            DateTime now = DateTime.Now;
            DateTime startTime = new DateTime(now.Year, now.Month, now.Day, 18, 0, 0);
            startTime = startTime.AddDays(1);
            startTime = TimeZoneInfo.ConvertTimeToUtc(startTime);
            serviceManager.SetupCalendarService(new List<EventModel>()
            {
                MockCalendarService.CreateEventModel(
                    startDateTime: startTime.AddDays(7),
                    endDateTime: startTime.AddDays(8))
            });

            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.FindMeetingByTimeRange)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.FoundOneEventPrompt("next week"))
                .AssertReply(this.ShowCalendarList(1))
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSummaryByStartTime()
        {
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(MockCalendarService.FakeDefaultEvents());
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.FindMeetingByStartTime)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.FoundOneEventPrompt("tomorrow"))
                .AssertReply(this.ShowCalendarList(1))
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }

        private string[] FoundOneEventPrompt(string dateTime = "today")
        {
            var responseParams = new StringDictionary()
            {
                { "Count", "1" },
                { "EventName1", Strings.Strings.DefaultEventName },
                { "EventDuration", "1 hour" },
                { "DateTime", dateTime },
                { "EventTime1", "at 6:00 PM" },
                { "Participants1", Strings.Strings.DefaultUserName }
            };

            return this.ParseReplies(SummaryResponses.ShowOneMeetingSummaryMessage.Replies, responseParams);
        }

        private string[] FoundMultipleEventPrompt(int count, string dateTime = "today")
        {
            var responseParams = new StringDictionary()
            {
                { "Count", count.ToString() },
                { "DateTime", dateTime },
                { "Participants1", Strings.Strings.DefaultUserName },
                { "EventName1", Strings.Strings.DefaultEventName },
                { "EventTime1", "at 6:00 PM" },
                { "Participants2", Strings.Strings.DefaultUserName },
                { "EventName2", Strings.Strings.DefaultEventName },
                { "EventTime2", "at 6:00 PM" },
            };

            return this.ParseReplies(SummaryResponses.ShowMultipleMeetingSummaryMessage.Replies, responseParams);
        }

        private Action<IActivity> ShowCalendarList(int count)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, count);
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

        private string[] ReadOutPrompt()
        {
            return this.ParseReplies(SummaryResponses.ReadOutPrompt.Replies, new StringDictionary());
        }

        private string[] AskForOrgnizerActionPrompt(string dateString = "today")
        {
            return this.ParseReplies(SummaryResponses.AskForOrgnizerAction.Replies, new StringDictionary() { { "DateTime", dateString } });
        }

        private Action<IActivity> ShowReadOutEventList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(
                    this.ParseReplies(SummaryResponses.ReadOutMessage.Replies, new StringDictionary()
                    {
                        {
                            "Date", DateTime.Now.AddDays(1).ToString(CommonStrings.DisplayDateFormat_CurrentYear)
                        },
                        {
                            "Time", "at 6:00 PM"
                        },
                        {
                            "Participants", Strings.Strings.DefaultUserName
                        },
                        {
                            "Subject", Strings.Strings.DefaultEventName
                        }
                    }), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private string[] NoEventResponse()
        {
            return this.ParseReplies(SummaryResponses.ShowNoMeetingMessage.Replies, new StringDictionary());
        }
    }
}
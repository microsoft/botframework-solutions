using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Summary.Resources;
using CalendarSkill.Dialogs.UpdateEvent.Resources;
using CalendarSkill.Models;
using CalendarSkillTest.Flow.Fakes;
using CalendarSkillTest.Flow.Utterances;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class SummaryCalendarFlowTests : CalendarBotTestBase
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
                    { "calendar", new MockLuisRecognizer(new FindMeetingTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarSummary()
        {
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
            this.ServiceManager = MockServiceManager.SetMeetingsToNull();
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
            this.ServiceManager = MockServiceManager.SetMeetingsToMultiple(eventCount);
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

        // [TestMethod]
        // public async Task Test_CalendarSummaryReadOutWithOneMeeting()
        // {
        //    await this.GetTestFlow()
        //        .Send(FindMeetingTestUtterances.BaseFindMeeting)
        //        .AssertReply(this.ShowAuth())
        //        .Send(this.GetAuthResponse())
        //        .AssertReplyOneOf(this.FoundOneEventPrompt())
        //        .AssertReply(this.ShowCalendarList(1))
        //        .Send(Strings.Strings.ConfirmYes)
        //        .AssertReply(this.ShowReadOutEventList())
        //        .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
        //        .Send(Strings.Strings.ConfirmNo)
        //        .AssertReply(this.ActionEndMessage())
        //        .StartTestAsync();
        // }

        // [TestMethod]
        // public async Task Test_CalendarSummaryReadOutWithMutipleMeeting()
        // {
        //    int eventCount = 3;
        //    this.ServiceManager = MockServiceManager.SetMeetingsToMultiple(eventCount);
        //    await this.GetTestFlow()
        //        .Send(FindMeetingTestUtterances.BaseFindMeeting)
        //        .AssertReply(this.ShowAuth())
        //        .Send(this.GetAuthResponse())
        //        .AssertReplyOneOf(this.FoundMultipleEventPrompt(eventCount))
        //        .AssertReply(this.ShowCalendarList(eventCount))
        //        .AssertReplyOneOf(this.ReadOutMorePrompt())
        //        .Send(FindMeetingTestUtterances.ChooseFirstMeeting)
        //        .AssertReply(this.ShowReadOutEventList())
        //        .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
        //        .Send(Strings.Strings.ConfirmNo)
        //        .AssertReply(this.ActionEndMessage())
        //        .StartTestAsync();
        // }
        [TestMethod]
        public async Task Test_CalendarSummaryByTimeRange()
        {
            DateTime now = DateTime.Now;
            DateTime nextWeekDay = now.AddDays(7);
            DateTime startTime = new DateTime(nextWeekDay.Year, nextWeekDay.Month, nextWeekDay.Day, 18, 0, 0);
            startTime = TimeZoneInfo.ConvertTimeToUtc(startTime);
            this.ServiceManager = MockServiceManager.SetMeetingsToSpecial(new List<EventModel>()
            {
                MockServiceManager.CreateEventModel(
                    startDateTime: startTime,
                    endDateTime: startTime.AddHours(1))
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

        // [TestMethod]
        // public async Task Test_CalendarSummaryShowOverviewAgain()
        // {
        //    await this.GetTestFlow()
        //        .Send(FindMeetingTestUtterances.BaseFindMeeting)
        //        .AssertReply(this.ShowAuth())
        //        .Send(this.GetAuthResponse())
        //        .AssertReplyOneOf(this.FoundOneEventPrompt())
        //        .AssertReply(this.ShowCalendarList(1))
        //        .Send(Strings.Strings.ConfirmYes)
        //        .AssertReply(this.ShowReadOutEventList())
        //        .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
        //        .Send(UpdateMeetingTestUtterances.BaseUpdateMeeting)
        //        .AssertReply(this.ShowAuth())
        //        .Send(this.GetAuthResponse())
        //        .AssertReplyOneOf(this.AskForNewTimePrompt())
        //        .Send(Strings.Strings.DefaultStartTime)
        //        .AssertReply(this.ShowUpdateCalendarList())
        //        .Send(Strings.Strings.ConfirmYes)
        //        .AssertReply(this.ShowUpdateCalendarList())
        //        .AssertReplyOneOf(this.AskForShowOverviewAgainPrompt())
        //        .Send(Strings.Strings.ConfirmYes)
        //        .AssertReply(this.ShowAuth())
        //        .Send(this.GetAuthResponse())
        //        .AssertReplyOneOf(this.FoundOneEventAgainPrompt())
        //        .AssertReply(this.ShowCalendarList(1))
        //        .Send(Strings.Strings.ConfirmNo)
        //        .AssertReply(this.ActionEndMessage())
        //        .StartTestAsync();
        // }
        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }

        private string[] ShowOneMeetingOverviewAgainResponse(string dateTime = "today")
        {
            var responseParams = new StringDictionary()
            {
                { "DateTime", dateTime }
            };

            return this.ParseReplies(SummaryResponses.ShowOneMeetingSummaryAgainMessage, responseParams);
        }

        private string[] ShowOverviewAgainResponse(int count, string dateTime = "today")
        {
            var responseParams = new StringDictionary()
            {
                { "Count", count.ToString() },
                { "DateTime", dateTime }
            };

            return this.ParseReplies(SummaryResponses.ShowMeetingSummaryAgainMessage, responseParams);
        }

        private string[] AskForShowOverviewAgainPrompt(string dateTime = "today")
        {
            var responseParams = new StringDictionary()
            {
                { "DateTime", dateTime }
            };

            return this.ParseReplies(SummaryResponses.AskForShowOverview, responseParams);
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

            return this.ParseReplies(SummaryResponses.ShowOneMeetingSummaryMessage, responseParams);
        }

        private string[] FoundOneEventAgainPrompt(string dateTime = "today")
        {
            var responseParams = new StringDictionary()
            {
                { "Count", "1" },
                { "DateTime", dateTime },
            };

            return this.ParseReplies(SummaryResponses.ShowOneMeetingSummaryAgainMessage, responseParams);
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

            return this.ParseReplies(SummaryResponses.ShowMultipleMeetingSummaryMessage, responseParams);
        }

        private Action<IActivity> ShowCalendarList(int count)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                //Assert.AreEqual(messageActivity.Attachments.Count, count);
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
            return this.ParseReplies(SummaryResponses.ReadOutMorePrompt, new StringDictionary());
        }

        private string[] ReadOutPrompt()
        {
            return this.ParseReplies(SummaryResponses.ReadOutPrompt, new StringDictionary());
        }

        private string[] AskForOrgnizerActionPrompt(string dateString = "today")
        {
            return this.ParseReplies(SummaryResponses.AskForOrgnizerAction, new StringDictionary() { { "DateTime", dateString } });
        }

        private Action<IActivity> ShowReadOutEventList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(
                    this.ParseReplies(SummaryResponses.ReadOutMessage, new StringDictionary()
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
            return this.ParseReplies(SummaryResponses.ShowNoMeetingMessage, new StringDictionary());
        }

        private string[] AskForNewTimePrompt()
        {
            return this.ParseReplies(UpdateEventResponses.NoNewTime, new StringDictionary());
        }

        private Action<IActivity> ShowUpdateCalendarList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }
    }
}
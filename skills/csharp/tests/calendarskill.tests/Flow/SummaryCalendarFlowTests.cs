using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Services;
using CalendarSkill.Test.Flow.Fakes;
using CalendarSkill.Test.Flow.Utterances;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkill.Test.Flow
{
    [TestClass]
    public class SummaryCalendarFlowTests : CalendarSkillTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var botServices = Services.BuildServiceProvider().GetService<BotServices>();
            botServices.CognitiveModelSets.Add("en", new CognitiveModelSet()
            {
                LuisServices = new Dictionary<string, LuisRecognizer>()
                {
                    { "General", new MockLuisRecognizer() },
                    { "Calendar", new MockLuisRecognizer(new FindMeetingTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarSummary()
        {
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseFindMeeting)
                .AssertReplyOneOf(this.FoundOneEventPrompt())
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSearchByTitle()
        {
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.FindMeetingByTitle)
                .AssertReplyOneOf(this.FoundOneEventAgainPrompt($"about {Strings.Strings.DefaultEventName}"))
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSearchByAttendee()
        {
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.FindMeetingByAttendee)
                .AssertReplyOneOf(this.FoundOneEventAgainPrompt($"with {Strings.Strings.DefaultUserName}"))
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSearchByLocation()
        {
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.FindMeetingByLocation)
                .AssertReplyOneOf(this.FoundOneEventAgainPrompt($"at {Strings.Strings.DefaultLocation}"))
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
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
                .AssertReplyOneOf(this.FoundMultipleEventPrompt(eventCount))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSummaryReadOutWithOneMeeting()
        {
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseFindMeeting)
                .AssertReplyOneOf(this.FoundOneEventPrompt())
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSummaryReadOutWithMutipleMeetingByNumber()
        {
            int eventCount = 3;
            this.ServiceManager = MockServiceManager.SetMeetingsToMultiple(eventCount);
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseFindMeeting)
                .AssertReplyOneOf(this.FoundMultipleEventPrompt(eventCount))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(GeneralTestUtterances.ChooseOne)
                .AssertReply(this.ShowReadOutEventList("0"))
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSummaryReadOutWithMutipleMeetingByTitle()
        {
            int eventCount = 3;
            this.ServiceManager = MockServiceManager.SetMeetingsToMultiple(eventCount);
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseFindMeeting)
                .AssertReplyOneOf(this.FoundMultipleEventPrompt(eventCount))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(Strings.Strings.DefaultEventName + "0")
                .AssertReply(this.ShowReadOutEventList("0"))
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSummaryReadOutWithMutipleMeetingByContactName()
        {
            int eventCount = 3;
            this.ServiceManager = MockServiceManager.SetMeetingsToMultiple(eventCount);
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseFindMeeting)
                .AssertReplyOneOf(this.FoundMultipleEventPrompt(eventCount))
                .AssertReplyOneOf(this.ReadOutMorePrompt())
                .Send(Strings.Strings.DefaultUserName + "0")
                .AssertReply(this.ShowReadOutEventList("0"))
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

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
                .AssertReplyOneOf(this.FoundOneEventPrompt("for next week", "next week"))
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt("next week"))
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSummaryByStartTime()
        {
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.FindMeetingByStartTime)
                .AssertReplyOneOf(this.FoundOneEventPrompt("tomorrow", "tomorrow"))
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt("tomorrow"))
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarSummaryShowOverviewAgain()
        {
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseFindMeeting)
                .AssertReplyOneOf(this.FoundOneEventPrompt())
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
                .Send(UpdateMeetingTestUtterances.BaseUpdateMeeting)
                .AssertReplyOneOf(this.AskForNewTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(this.ShowUpdateCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowUpdateCalendarList())
                .AssertReplyOneOf(this.AskForShowOverviewAgainPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.FoundOneEventAgainPrompt())
                .AssertReplyOneOf(this.AskForOrgnizerActionPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Handoff);
            };
        }

        private string[] AskForShowOverviewAgainPrompt(string dateTime = "today")
        {
            var responseParams = new StringDictionary()
            {
                { "DateTime", dateTime }
            };

            return this.ParseReplies(SummaryResponses.AskForShowOverview, responseParams);
        }

        private string[] FoundOneEventPrompt(string conditionString = "today", string dateTime = "today")
        {
            var responseParams = new StringDictionary()
            {
                { "Condition", conditionString },
                { "Count", "1" },
                { "EventName1", Strings.Strings.DefaultEventName },
                { "EventDuration", "1 hour" },
                { "DateTime", dateTime },
                { "EventTime1", "at 6:00 PM" },
                { "Participants1", Strings.Strings.DefaultUserName }
            };

            return this.ParseReplies(SummaryResponses.ShowOneMeetingSummaryMessage, responseParams);
        }

        private string[] FoundOneEventAgainPrompt(string conditionString = "today")
        {
            var responseParams = new StringDictionary()
            {
                { "Condition", conditionString },
                { "Count", "1" },
            };

            return this.ParseReplies(SummaryResponses.ShowOneMeetingSummaryShortMessage, responseParams);
        }

        private string[] FoundMultipleEventPrompt(int count, string conditionString = "today", string dateTime = "today")
        {
            var responseParams = new StringDictionary()
            {
                { "Condition", conditionString },
                { "Count", count.ToString() },
                { "DateTime", dateTime },
                { "Participants1", Strings.Strings.DefaultUserName + "0" },
                { "EventName1", Strings.Strings.DefaultEventName + "0" },
                { "EventTime1", "at 6:00 PM" },
                { "Participants2", Strings.Strings.DefaultUserName + (count - 1).ToString() },
                { "EventName2", Strings.Strings.DefaultEventName + (count - 1).ToString() },
                { "EventTime2", "at 6:00 PM" },
            };

            return this.ParseReplies(SummaryResponses.ShowMultipleMeetingSummaryMessage, responseParams);
        }

        private string[] ReadOutMorePrompt()
        {
            return this.ParseReplies(SummaryResponses.ReadOutMorePrompt, new StringDictionary());
        }

        private string[] AskForOrgnizerActionPrompt(string dateString = "today")
        {
            return this.ParseReplies(SummaryResponses.AskForOrgnizerAction, new StringDictionary() { { "DateTime", dateString } });
        }

        private Action<IActivity> ShowReadOutEventList(string suffix = "")
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
                            "Participants", Strings.Strings.DefaultUserName + suffix
                        },
                        {
                            "Subject", Strings.Strings.DefaultEventName + suffix
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
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.NextMeeting.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalendarSkillTest.Flow.Utterances;
using CalendarSkillTest.Flow.Fakes;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Builder;
using CalendarSkill;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class NextCalendarFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            this.Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, IRecognizer>()
                {
                    { "general", new MockLuisRecognizer() },
                    { "calendar", new MockLuisRecognizer(new FindMeetingTestUtterances()) }
                }
            });

            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(MockCalendarService.FakeDefaultEvents());
            serviceManager.SetupUserService(MockUserService.FakeDefaultUsers(), MockUserService.FakeDefaultPeople());
        }

        [TestMethod]
        public async Task Test_CalendarOneNextMeeting()
        {
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseNextMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.NextMeetingPrompt())
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarNoNextMeetings()
        {
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(new List<EventModel>());
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseNextMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.NoMeetingResponse())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarMultipleMeetings()
        {
            int eventCount = 3;
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(MockCalendarService.FakeMultipleNextEvents(eventCount));
            await this.GetTestFlow()
                .Send(FindMeetingTestUtterances.BaseNextMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.NextMeetingPrompt())
                .AssertReply(this.ShowCalendarList(eventCount))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] NextMeetingPrompt()
        {
            return this.ParseReplies(NextMeetingResponses.ShowNextMeetingMessage.Replies, new StringDictionary());
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
            };
        }

        private Action<IActivity> ShowCalendarList(int eventCount = 1)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, eventCount);
            };
        }

        private string[] NoMeetingResponse()
        {
            return this.ParseReplies(NextMeetingResponses.ShowNoMeetingMessage.Replies, new StringDictionary());
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

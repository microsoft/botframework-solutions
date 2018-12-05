using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalendarSkillTest.Flow.Utterances;
using CalendarSkillTest.Flow.Fakes;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class CreateCalendarFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            this.Services.LuisServices.Add("calendar", new MockLuisRecognizer(new CreateMeetingTestUtterances()));
        }

        [TestMethod]
        public async Task Test_CalendarCreate()
        {
            await this.GetTestFlow()
                    .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                    .AssertReply(this.ShowAuth())
                    .Send(this.GetAuthResponse())
                    .AssertReplyOneOf(this.AskForParticpantsPrompt())
                    .Send("test@test.com")
                    .AssertReplyOneOf(this.AskForSubjectPrompt())
                    .Send("test subject")
                    .AssertReplyOneOf(this.AskForContentPrompt())
                    .Send("test content")
                    .AssertReplyOneOf(this.AskForDatePrompt())
                    .Send("tomorrow")
                    .AssertReplyOneOf(this.AskForStartTimePrompt())
                    .Send("at 9 AM")
                    .AssertReplyOneOf(this.AskForDurationPrompt())
                    .Send("one hour")
                    .AssertReplyOneOf(this.AskForLocationPrompt())
                    .Send("office")
                    .AssertReply(this.ShowCalendarList())
                    .Send("Yes")
                    .AssertReply(this.ShowCalendarList())
                    .StartTestAsync();
        }

        private string[] WelcomePrompt()
        {
            return this.ParseReplies(CalendarMainResponses.CalendarWelcomeMessage.Replies, new StringDictionary());
        }

        private string[] AskForParticpantsPrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoAttendeesMS.Replies, new StringDictionary());
        }

        private string[] AskForSubjectPrompt()
        {
            var responseParams = new StringDictionary()
            {
                { "UserName", "test@test.com" },
            };

            return this.ParseReplies(CreateEventResponses.NoTitle.Replies, responseParams);
        }

        private string[] AskForContentPrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoContent.Replies, new StringDictionary());
        }

        private string[] AskForDatePrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoStartDate.Replies, new StringDictionary());
        }

        private string[] AskForStartTimePrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoStartTime.Replies, new StringDictionary());
        }

        private string[] AskForDurationPrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoDuration.Replies, new StringDictionary());
        }

        private string[] AskForLocationPrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoLocation.Replies, new StringDictionary());
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
            };
        }

        private Action<IActivity> ShowCalendarList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }
    }
}

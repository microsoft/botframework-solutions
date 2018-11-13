using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class CreateCalendarFlowTests : CalendarBotTestBase
    {
        [TestMethod]
        public async Task Test_CalendarCreate()
        {
            await this.GetTestFlow()
                .Send("Create a meeting")
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

        private string[] ActionEndMessage()
        {
            return this.ParseReplies(CalendarSharedResponses.CancellingMessage.Replies, new StringDictionary());
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

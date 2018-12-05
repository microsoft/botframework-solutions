using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.UpdateEvent.Resources;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalendarSkillTest.Flow.Utterances;
using CalendarSkillTest.Flow.Fakes;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class UpdateCalendarFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            this.Services.LuisServices.Add("calendar", new MockLuisRecognizer(new UpdateMeetingTestUtterances()));
        }

        [TestMethod]
        public async Task Test_CalendarCreate()
        {
            await this.GetTestFlow()
                .Send(UpdateMeetingTestUtterances.BaseUpdateMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForTitleTimePrompt())
                .Send("test subject")
                .AssertReplyOneOf(this.AskForNewTimePrompt())
                .Send("tomorrow 9 PM")
                .AssertReply(this.ShowCalendarList())
                .Send("Yes")
                .AssertReply(this.ShowCalendarList())
                .StartTestAsync();
        }

        private string[] WelcomePrompt()
        {
            return this.ParseReplies(CalendarMainResponses.CalendarWelcomeMessage.Replies, new StringDictionary());
        }

        private string[] AskForTitleTimePrompt()
        {
            return this.ParseReplies(UpdateEventResponses.NoUpdateStartTime.Replies, new StringDictionary());
        }

        private string[] AskForNewTimePrompt()
        {
            return this.ParseReplies(UpdateEventResponses.NoNewTime.Replies, new StringDictionary());
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

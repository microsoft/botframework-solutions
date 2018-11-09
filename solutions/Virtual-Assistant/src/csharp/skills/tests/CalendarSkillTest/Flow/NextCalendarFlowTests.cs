using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.NextMeeting.Resources;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class NextCalendarFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
        }

        [TestMethod]
        public async Task Test_CalendarDelete()
        {
            await this.GetTestFlow()
                .Send("what is my next meeting")
                .AssertReplyOneOf(this.NextMeetingPrompt())
                .AssertReply(this.ShowCalendarList())
                .StartTestAsync();
        }

        private string[] ActionEndMessage()
        {
            return this.ParseReplies(CalendarSharedResponses.CancellingMessage.Replies, new StringDictionary());
        }

        private string[] NextMeetingPrompt()
        {
            return this.ParseReplies(NextMeetingResponses.ShowNextMeetingMessage.Replies, new StringDictionary());
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

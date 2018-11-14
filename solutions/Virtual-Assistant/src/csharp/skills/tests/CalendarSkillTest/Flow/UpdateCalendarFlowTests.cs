using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.UpdateEvent.Resources;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class UpdateCalendarFlowTests : CalendarBotTestBase
    {
        [TestMethod]
        public async Task Test_CalendarCreate()
        {
            await this.GetTestFlow()
                .Send(GetTriggerActivity())
                .AssertReplyOneOf(this.WelcomePrompt())
                .Send("update meeting")
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

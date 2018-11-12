using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.DeleteEvent.Resources;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class DeleteCalendarFlowTests : CalendarBotTestBase
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
                .Send("delete meeting")
                .AssertReplyOneOf(this.AskForDeletePrompt())
                .Send("test subject")
                .AssertReply(this.ShowCalendarList())
                .Send("Yes")
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .StartTestAsync();
        }


        private string[] AskForDeletePrompt()
        {
            return this.ParseReplies(DeleteEventResponses.NoDeleteStartTime.Replies, new StringDictionary());
        }

        private string[] DeleteEventPrompt()
        {
            return this.ParseReplies(DeleteEventResponses.EventDeleted.Replies, new StringDictionary());
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

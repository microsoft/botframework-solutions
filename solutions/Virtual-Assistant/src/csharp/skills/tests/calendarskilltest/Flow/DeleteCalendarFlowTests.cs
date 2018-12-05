using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.DeleteEvent.Resources;
using CalendarSkill.Dialogs.Main.Resources;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalendarSkillTest.Flow.Utterances;
using CalendarSkillTest.Flow.Fakes;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class DeleteCalendarFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            this.Services.LuisServices.Add("calendar", new MockLuisRecognizer(new DeleteMeetingTestUtterances()));
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.FakeEventList = MockCalendarService.FakeDefaultEvents();
            serviceManager.FakeUserList = MockUserService.FakeDefaultUsers();
            serviceManager.FakePeopleList = MockUserService.FakeDefaultPeople();
        }

        [TestMethod]
        public async Task Test_CalendarDelete()
        {
            await this.GetTestFlow()
                .Send(DeleteMeetingTestUtterances.BaseDeleteMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForDeletePrompt())
                .Send("test subject")
                .AssertReply(this.ShowCalendarList())
                .Send("Yes")
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .StartTestAsync();
        }

        private string[] WelcomePrompt()
        {
            return this.ParseReplies(CalendarMainResponses.CalendarWelcomeMessage.Replies, new StringDictionary());
        }

        private string[] AskForDeletePrompt()
        {
            return this.ParseReplies(DeleteEventResponses.NoDeleteStartTime.Replies, new StringDictionary());
        }

        private string[] DeleteEventPrompt()
        {
            return this.ParseReplies(DeleteEventResponses.EventDeleted.Replies, new StringDictionary());
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

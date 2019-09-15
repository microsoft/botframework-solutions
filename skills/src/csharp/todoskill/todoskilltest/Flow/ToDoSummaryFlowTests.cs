using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Responses.ShowToDo;
using ToDoSkillTest.Flow.Fakes;
using ToDoSkillTest.Flow.Utterances;

namespace ToDoSkillTest.Flow
{
    [TestClass]
    public class ToDoSummaryFlowTests : ToDoBotTestBase
    {
        [TestMethod]
        public async Task Test_ToDoSummary()
        {
            await this.GetTestFlow()
                .Send(this.SendSummaryEvent())
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ActionEndMessage())
                .AssertReply(this.ActionEndMessage())
                .AssertReply(this.ActionHandoff())
                .StartTestAsync();
        }

        public Activity SendSummaryEvent()
        {
            return new Activity(ActivityTypes.Event, name: "SummaryEvent");
        }

        private Action<IActivity> ShowAuth()
        {
            return activity =>
            {
                var message = activity.AsMessageActivity();
                Assert.AreEqual(1, message.Attachments.Count);
                Assert.AreEqual("application/vnd.microsoft.card.oauth", message.Attachments[0].ContentType);
            };
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Message);
            };
        }

        private Action<IActivity> ActionHandoff()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Handoff);
            };
        }
    }
}
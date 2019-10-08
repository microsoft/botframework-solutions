using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Services;
using CalendarSkill.Test.Flow.Fakes;
using CalendarSkill.Test.Flow.Utterances;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkill.Test.Flow
{
    [TestClass]
    public class UpdateCalendarFlowTests : CalendarSkillTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var botServices = Services.BuildServiceProvider().GetService<BotServices>();
            botServices.CognitiveModelSets.Add("en", new CognitiveModelSet()
            {
                LuisServices = new Dictionary<string, ITelemetryRecognizer>()
                {
                    { "General", new MockLuisRecognizer() },
                    { "Calendar", new MockLuisRecognizer(new UpdateMeetingTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarUpdateWithStartTimeEntity()
        {
            await this.GetTestFlow()
                .Send(UpdateMeetingTestUtterances.UpdateMeetingWithStartTime)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForNewTimePrompt())
                .Send("tomorrow 9 pm")
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.UpdateEventPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarUpdateWithTitleEntity()
        {
            await this.GetTestFlow()
                .Send(UpdateMeetingTestUtterances.UpdateMeetingWithTitle)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForNewTimePrompt())
                .Send("tomorrow 9 pm")
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.UpdateEventPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarUpdateWithMoveEarlierTimeSpan()
        {
            await this.GetTestFlow()
                .Send(UpdateMeetingTestUtterances.UpdateMeetingWithMoveEarlierTimeSpan)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.UpdateEventPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarUpdateWithMoveLaterTimeSpan()
        {
            await this.GetTestFlow()
                .Send(UpdateMeetingTestUtterances.UpdateMeetingWithMoveLaterTimeSpan)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.UpdateEventPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarUpdateFromMultipleEvents()
        {
            int eventCount = 3;
            this.ServiceManager = MockServiceManager.SetMeetingsToMultiple(eventCount);
            await this.GetTestFlow()
                .Send(UpdateMeetingTestUtterances.UpdateMeetingWithStartTime)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowCalendarList())
                .Send(GeneralTestUtterances.ChooseOne)
                .AssertReplyOneOf(this.AskForNewTimePrompt())
                .Send("tomorrow 9 pm")
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.UpdateEventPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] AskForNewTimePrompt()
        {
            return this.ParseReplies(UpdateEventResponses.NoNewTime, new StringDictionary());
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

        private string[] UpdateEventPrompt()
        {
            return this.ParseReplies(UpdateEventResponses.EventUpdated, new StringDictionary());
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Handoff);
            };
        }
    }
}
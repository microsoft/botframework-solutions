using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Responses.ChangeEventStatus;
using CalendarSkill.Services;
using CalendarSkillTest.Flow.Fakes;
using CalendarSkillTest.Flow.Utterances;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class ChangeCalendarStatusFlowTests : CalendarBotTestBase
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
                    { "Calendar", new MockLuisRecognizer(new ChangeMeetingStatusTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarDeleteWithStartTimeEntity()
        {
            await this.GetTestFlow()
                .Send(ChangeMeetingStatusTestUtterances.DeleteMeetingWithStartTime)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarDeleteWithTitleEntity()
        {
            await this.GetTestFlow()
                .Send(ChangeMeetingStatusTestUtterances.DeleteMeetingWithTitle)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarDeleteFromMultipleEvents()
        {
            int eventCount = 3;
            this.ServiceManager = MockServiceManager.SetMeetingsToMultiple(eventCount);
            await this.GetTestFlow()
                .Send(ChangeMeetingStatusTestUtterances.DeleteMeetingWithTitle)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowCalendarList())
                .Send(GeneralTestUtterances.ChooseOne)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.DeleteEventPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarAcceptWithStartTimeEntity()
        {
            await this.GetTestFlow()
                .Send(ChangeMeetingStatusTestUtterances.AcceptMeetingWithStartTime)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.AcceptEventPrompt())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] DeleteEventPrompt()
        {
            return this.ParseReplies(ChangeEventStatusResponses.EventDeleted, new StringDictionary());
        }

        private string[] AcceptEventPrompt()
        {
            return this.ParseReplies(ChangeEventStatusResponses.EventAccepted, new StringDictionary());
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

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }
    }
}
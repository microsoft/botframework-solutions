﻿
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Responses.FindMeetingRoom;
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
    public class UpdateMeetingRoomFlowTests : CalendarSkillTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var botServices = Services.BuildServiceProvider().GetService<BotServices>();
            botServices.CognitiveModelSets.Add("en-us", new CognitiveModelSet()
            {
                LuisServices = new Dictionary<string, LuisRecognizer>()
                {
                    { "General", new MockLuisRecognizer() },
                    { "Calendar", new MockLuisRecognizer(new UpdateMeetingRoomTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_ChangeMeetingRoomWithNewStartDateEntity()
        {
            this.ServiceManager = MockServiceManager.SetMeetingWithMeetingRoom();
            await this.GetTestFlow()
                .Send(UpdateMeetingRoomTestUtterances.ChangeMeetingRoomWithStartTime)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(dateTime: "at 6:00 PM"))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(ReplyMeetingRoomChanged(dateTime: "at 6:00 PM"))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AddMeetingRoomWithNewStartDateEntity()
        {
            this.ServiceManager = MockServiceManager.SetMeetingWithMeetingRoom();
            await this.GetTestFlow()
                .Send(UpdateMeetingRoomTestUtterances.AddMeetingRoomWithStartTime)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(dateTime: "at 6:00 PM"))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(ReplyMeetingRoomAdded(dateTime: "at 6:00 PM"))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CancelMeetingRoomWithNewStartDateEntity()
        {
            this.ServiceManager = MockServiceManager.SetMeetingWithMeetingRoom();
            await this.GetTestFlow()
                .Send(UpdateMeetingRoomTestUtterances.CancelMeetingRoomWithStartTime)
                .AssertReplyOneOf(ReplyMeetingRoomCanceled(dateTime: "at 6:00 PM"))
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] AskForBuildingPrompt()
        {
            return GetTemplates(FindMeetingRoomResponses.NoBuilding);
        }

        private string[] AskForFloorNumberPrompt()
        {
            return GetTemplates(FindMeetingRoomResponses.NoFloorNumber);
        }

        private string[] AskForConfirmMeetingRoomPrompt(int roomNumber = 1, string dateTime = "right now")
        {
            return GetTemplates(FindMeetingRoomResponses.ConfirmMeetingRoomPrompt, new
            {
                MeetingRoom = string.Format(Strings.Strings.MeetingRoomName, roomNumber),
                DateTime = dateTime
            });
        }

        private string[] ReplyMeetingRoomChanged(
            string meetingRoom = Strings.Strings.DefaultMeetingRoomName,
            string dateTime = "right now",
            string subject = Strings.Strings.DefaultEventName)
        {
            return GetTemplates(FindMeetingRoomResponses.MeetingRoomChanged, new
            {
                MeetingRoom = meetingRoom,
                DateTime = dateTime,
                Subject = subject
            });
        }

        private string[] ReplyMeetingRoomAdded(
            string meetingRoom = Strings.Strings.DefaultMeetingRoomName,
            string dateTime = "right now",
            string subject = Strings.Strings.DefaultEventName)
        {
            return GetTemplates(FindMeetingRoomResponses.MeetingRoomAdded, new
            {
                MeetingRoom = meetingRoom,
                DateTime = dateTime,
                Subject = subject
            });
        }

        private string[] ReplyMeetingRoomCanceled(
            string meetingRoom = Strings.Strings.DefaultMeetingRoomName,
            string dateTime = "right now",
            string subject = Strings.Strings.DefaultEventName)
        {
            return GetTemplates(FindMeetingRoomResponses.MeetingRoomCanceled, new
            {
                MeetingRoom = meetingRoom,
                DateTime = dateTime,
                Subject = subject
            });
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
            return GetTemplates(UpdateEventResponses.EventUpdated);
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
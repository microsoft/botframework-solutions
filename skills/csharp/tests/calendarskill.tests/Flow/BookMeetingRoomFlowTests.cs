// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.FindContact;
using CalendarSkill.Responses.FindMeetingRoom;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Test.Flow.Fakes;
using CalendarSkill.Test.Flow.Utterances;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkill.Test.Flow
{
    [TestClass]
    public class BookMeetingRoomFlowTests : CalendarSkillTestBase
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
                    { "Calendar", new MockLuisRecognizer(new BookMeetingRoomTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom()
        {
            await GetTestFlow()
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(ConfirmedMeetingRoom())
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_OneFloor()
        {
            MockSearchClient.SetSingleMeetingRoom();
            await GetTestFlow()
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(ConfirmedMeetingRoom())
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_ChangeRoom()
        {
            await GetTestFlow()
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(IgnoreMeetingRoom())
                .AssertReplyOneOf(AskForChangePrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeMeetingRoom)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(Strings.Strings.DefaultMeetingRoom2))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(ConfirmedMeetingRoom())
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        private string[] ConfirmOneNameOneAddress(string name = Strings.Strings.DefaultUserName, string address = Strings.Strings.DefaultUserEmail)
        {
            return GetTemplates(FindContactResponses.PromptOneNameOneAddress, new
            {
                User = $"{name} ({address})"
            });
        }

        private string[] AddMoreUserPrompt()
        {
            return GetTemplates(FindContactResponses.AddMoreUserPrompt);
        }

        private string[] AskForParticpantsPrompt()
        {
            return GetTemplates(FindContactResponses.AddMoreAttendees);
        }

        private string[] AskForSubjectWithContactNamePrompt(string userName = null, string userEmail = null)
        {
            return GetTemplates(CreateEventResponses.NoTitle, new
            {
                UserName = userName ?? Strings.Strings.DefaultUserName
            });
        }

        private string[] AskForBuildingPrompt()
        {
            return GetTemplates(FindMeetingRoomResponses.NoBuilding);
        }

        private string[] AskForFloorNumberPrompt()
        {
            return GetTemplates(FindMeetingRoomResponses.NoFloorNumber);
        }

        private string[] AskForConfirmMeetingRoomPrompt(string meetingRoom = Strings.Strings.DefaultMeetingRoom)
        {
            return GetTemplates(FindMeetingRoomResponses.ConfirmMeetingRoomPrompt, new
            {
                MeetingRoom = meetingRoom,
                DateTime = "right now"
            });
        }

        private string[] AskForChangePrompt()
        {
            return GetTemplates(FindMeetingRoomResponses.RecreateMeetingRoom);
        }

        private string[] AskForContentPrompt()
        {
            return GetTemplates(CreateEventResponses.NoContent);
        }
        private string[] AskForDurationPrompt()
        {
            return GetTemplates(CreateEventResponses.NoDuration);
        }

        private Action<IActivity> ShowCalendarList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private string[] ConfirmPrompt()
        {
            return GetTemplates(CreateEventResponses.ConfirmCreatePrompt);
        }

        private string[] BookedMeeting()
        {
            return GetTemplates(CreateEventResponses.MeetingBooked);
        }

        private string[] IgnoreMeetingRoom()
        {
            return GetTemplates(FindMeetingRoomResponses.IgnoreMeetingRoom);
        }
        private string[] ConfirmedMeetingRoom()
        {
            return GetTemplates(FindMeetingRoomResponses.ConfirmedMeetingRoom);
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
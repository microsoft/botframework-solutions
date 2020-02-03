// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.FindContact;
using CalendarSkill.Responses.FindMeetingRoom;
using CalendarSkill.Responses.Main;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Test.Flow.Fakes;
using CalendarSkill.Test.Flow.Utterances;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkill.Test.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
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
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_RetryBuilding()
        {
            string buildingNonexistent = string.Format(Strings.Strings.Building, 0);
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(buildingNonexistent)
                .AssertReplyOneOf(AskForBuildingRetryPrompt())
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_RetryBuilding_Fail()
        {
            string buildingNonexistent = string.Format(Strings.Strings.Building, 0);
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(buildingNonexistent)
                .AssertReplyOneOf(AskForBuildingRetryPrompt())
                .Send(buildingNonexistent)
                .AssertReplyOneOf(AskForBuildingRetryPrompt())
                .Send(buildingNonexistent)
                .AssertReplyOneOf(AskForBuildingRetryPrompt())
                .Send(buildingNonexistent)
                .AssertReplyOneOf(ReplyRetryTooManyResponse())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_RetryFloorNumber()
        {
            string floorNumberNonexistent = string.Format(Strings.Strings.FloorNumber, "no number");
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(floorNumberNonexistent)
                .AssertReplyOneOf(AskForFloorNumberRetryPrompt())
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_RetryFloorNumber_Fail()
        {
            string floorNumberNonexistent = string.Format(Strings.Strings.FloorNumber, "no number");
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(floorNumberNonexistent)
                .AssertReplyOneOf(AskForFloorNumberRetryPrompt())
                .Send(floorNumberNonexistent)
                .AssertReplyOneOf(AskForFloorNumberRetryPrompt())
                .Send(floorNumberNonexistent)
                .AssertReplyOneOf(AskForFloorNumberRetryPrompt())
                .Send(floorNumberNonexistent)
                .AssertReplyOneOf(ReplyRetryTooManyResponse())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_MeetingRoomNotFoundByBuildingAndRecreate()
        {
            var floorNumberNonexistent = 10;
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(string.Format(Strings.Strings.FloorNumber, floorNumberNonexistent))
                .AssertReplyOneOf(ReplyMeetingRoomNotFoundByBuilding(floorNumber: floorNumberNonexistent))
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeMeetingRoom)
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_MeetingRoomBusyAndRecreate()
        {
            var floorNumberBusy = 2;
            ServiceManager = MockServiceManager.SetFloor2NotAvailable();
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(string.Format(Strings.Strings.FloorNumber, floorNumberBusy))
                .AssertReplyOneOf(ReplyMeetingRoomBusy(floorNumber: floorNumberBusy))
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeMeetingRoom)
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_MeetingRoomRejectedAndRecreate()
        {
            ServiceManager = MockServiceManager.SetFloor2NotAvailable();
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(ReplyMeetingRoomIgnored())
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeMeetingRoom)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(2))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_MeetingRoomRejectedAndNoOtherRooms()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(ReplyMeetingRoomIgnored())
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeMeetingRoom)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(2))
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(ReplyMeetingRoomIgnored())
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeMeetingRoom)
                .AssertReplyOneOf(ReplyNotFindOtherMeetingRoom())
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.CancelRequest)
                .AssertReplyOneOf(ReplyCancelRequest())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_SingleMeetingRoom()
        {
            MockSearchClient.SetSingleMeetingRoom();
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_ChangeRoom()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
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
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeMeetingRoom)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(2))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_ChangeRoom_Fail()
        {
            MockSearchClient.SetSingleMeetingRoom();
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(IgnoreMeetingRoom())
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeMeetingRoom)
                .AssertReplyOneOf(ReplyNotFindOtherMeetingRoom())
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.CancelRequest)
                .AssertReplyOneOf(ReplyCancelRequest())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_ChangeRoomWithFloorNumber()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(string.Format(Strings.Strings.FloorNumber, 10))
                .AssertReplyOneOf(ReplyMeetingRoomNotFoundByBuilding(floorNumber: 10))
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeMeetingRoomWithFloorNumberEntity)
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_ChangeRoomWithBuilding()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(string.Format(Strings.Strings.Building, 2))
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(string.Format(Strings.Strings.FloorNumber, 2))
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(7))
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(ReplyMeetingRoomIgnored())
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.BookMeetingRoomWithBuildingEntity)
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_ChangeRoomWithRoomName()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(string.Format(Strings.Strings.Building, 2))
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(string.Format(Strings.Strings.FloorNumber, 2))
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(7))
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(ReplyMeetingRoomIgnored())
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.BookMeetingRoomWithMeetingRoomEntity)
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_ChangeTime()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(ReplyMeetingRoomIgnored())
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeTime)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(dateTime: "at 9:00 AM"))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_ChangeTimeWithDate()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(ReplyMeetingRoomIgnored())
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeTimeWithDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(dateTime: "at 9:00 AM"))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_ChangeTimeWithTime()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(ReplyMeetingRoomIgnored())
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeTimeWithTime)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(dateTime: "at 9:00 PM"))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_ChangeTimeWithDateAndTime()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BaseBookMeetingRoom)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(ReplyMeetingRoomIgnored())
                .AssertReplyOneOf(AskForRecreateMeetingRoomPrompt())
                .Send(BookMeetingRoomTestUtterances.ChangeTimeWithDateAndTime)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(dateTime: "at 9:00 AM"))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_WithRoomName()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BookMeetingRoomWithMeetingRoomEntity)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_WithBuilding()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BookMeetingRoomWithBuildingEntity)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_WithBuildingAndFloorNumber()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BookMeetingRoomWithBuildingAndFloorNumberEntity)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_WithDateTime()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BookMeetingRoomWithDateTimeEntity)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(dateTime: "at 9:00 AM"))
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
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_BookMeetingRoom_WithStartDateTime()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(BookMeetingRoomTestUtterances.BookMeetingRoomWithStartDateTimeEntity)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(dateTime: "at 9:00 AM"))
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

        private string[] AskForContentPrompt()
        {
            return GetTemplates(CreateEventResponses.NoContent);
        }

        private string[] AskForDatePrompt()
        {
            return GetTemplates(CreateEventResponses.NoStartDate);
        }

        private string[] AskForStartTimePrompt()
        {
            return GetTemplates(CreateEventResponses.NoStartTime);
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

        private string[] CanceledRequest()
        {
            return GetTemplates(CalendarMainResponses.CancelMessage);
        }

        private string[] AskForBuildingPrompt()
        {
            return GetTemplates(FindMeetingRoomResponses.NoBuilding);
        }

        private string[] AskForBuildingRetryPrompt()
        {
            return GetTemplates(FindMeetingRoomResponses.BuildingNonexistent);
        }

        private string[] AskForFloorNumberPrompt()
        {
            return GetTemplates(FindMeetingRoomResponses.NoFloorNumber);
        }

        private string[] AskForFloorNumberRetryPrompt()
        {
            return GetTemplates(FindMeetingRoomResponses.FloorNumberRetry);
        }

        private string[] AskForConfirmMeetingRoomPrompt(int roomNumber = 1, string dateTime = "right now")
        {
            // check for both passed dateTime (if it exists) AND "right now" date time to support dateTime difference on CI machines
            string[] acceptableResponseOne = GetTemplates(FindMeetingRoomResponses.ConfirmMeetingRoomPrompt, new
            {
                MeetingRoom = string.Format(Strings.Strings.MeetingRoomName, roomNumber),
                DateTime = dateTime
            });

            if (dateTime == "right now")
            {
                return acceptableResponseOne;
            }

            string[] acceptableResponseTwo = GetTemplates(FindMeetingRoomResponses.ConfirmMeetingRoomPrompt, new
            {
                MeetingRoom = string.Format(Strings.Strings.MeetingRoomName, roomNumber),
                DateTime = "right now"
            });

            string[] combined = new string[acceptableResponseOne.Length + acceptableResponseTwo.Length];
            Array.Copy(acceptableResponseOne, combined, acceptableResponseOne.Length);
            Array.Copy(acceptableResponseTwo, 0, combined, acceptableResponseOne.Length, acceptableResponseTwo.Length);
            return combined;
        }

        private string[] AskForRecreateMeetingRoomPrompt()
        {
            return GetTemplates(FindMeetingRoomResponses.RecreateMeetingRoom);
        }

        private string[] IgnoreMeetingRoom()
        {
            return GetTemplates(FindMeetingRoomResponses.IgnoreMeetingRoom);
        }

        private string[] ConfirmedMeetingRoom()
        {
            return GetTemplates(FindMeetingRoomResponses.ConfirmedMeetingRoom);
        }

        private string[] ReplyRetryTooManyResponse()
        {
            return GetTemplates(CalendarSharedResponses.RetryTooManyResponse);
        }

        private string[] ReplyMeetingRoomNotFound()
        {
            return GetTemplates(FindMeetingRoomResponses.MeetingRoomNotFoundByName, new
            {
                MeetingRoom = Strings.Strings.DefaultMeetingRoomName,
            });
        }

        private string[] ReplyMeetingRoomNotFoundByBuilding(
            string building = Strings.Strings.DefaultBuilding,
            int floorNumber = 1,
            string dateTime = "right now")
        {
            return GetTemplates(FindMeetingRoomResponses.MeetingRoomNotFoundByBuildingAndFloor, new
            {
                Building = building,
                FloorNumber = floorNumber,
                DateTime = dateTime
            });
        }

        private string[] ReplyMeetingRoomBusy(
            string building = Strings.Strings.DefaultBuilding,
            int floorNumber = 1,
            string dateTime = "right now")
        {
            return GetTemplates(FindMeetingRoomResponses.MeetingRoomNotFoundByBuildingAndFloor, new
            {
                Building = building,
                FloorNumber = floorNumber,
                DateTime = dateTime
            });
        }

        private string[] ReplyNotFindOtherMeetingRoom(
            string building = Strings.Strings.DefaultBuilding,
            int floorNumber = 1,
            string dateTime = "right now")
        {
            return GetTemplates(FindMeetingRoomResponses.CannotFindOtherMeetingRoom, new
            {
                Building = building,
                FloorNumber = floorNumber,
                DateTime = dateTime
            });
        }

        private string[] ReplyMeetingRoomIgnored()
        {
            return GetTemplates(FindMeetingRoomResponses.IgnoreMeetingRoom);
        }

        private string[] ReplyCancelRequest()
        {
            return GetTemplates(FindMeetingRoomResponses.CancelRequest);
        }
    }
}
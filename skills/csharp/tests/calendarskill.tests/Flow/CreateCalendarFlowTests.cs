// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkill.Test.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class CreateCalendarFlowTests : CalendarSkillTestBase
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
                    { "Calendar", new MockLuisRecognizer(new CreateMeetingTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarCreate()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_RetryTooMany()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send("test")
                .AssertReplyOneOf(AskForRecreateInfoReprompt())
                .Send("test")
                .AssertReplyOneOf(AskForRecreateInfoReprompt())
                .Send("test")
                .AssertReplyOneOf(AskForRecreateInfoReprompt())
                .Send("test")
                .AssertReplyOneOf(RetryTooManyResponse())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeTime()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithTime)

                // test limitation for now. need to do further investigate.
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeDuration()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithDuration)

                // test limitation for now. need to do further investigate.
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeLocation()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithLocation)

                // test limitation for now. need to do further investigate.
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeParticipants()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithParticipants)

                // test limitation for now. need to do further investigate.
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeSubject()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithSubject)

                // test limitation for now. need to do further investigate.
                .AssertReplyOneOf(AskForSubjectShortPrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeContent()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithContent)

                // test limitation for now. need to do further investigate.
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithEmailAddress()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserEmail)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(Strings.Strings.DefaultUserEmail))
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithEmailAddressPrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithMultipleContacts()
        {
            var testDupRecipient = string.Format(Strings.Strings.UserName, 0);
            var testDupEmailAddress = string.Format(Strings.Strings.UserEmailAddress, 0);
            var testRecipient = Strings.Strings.DefaultUserName;
            var testEmailAddress = Strings.Strings.DefaultUserEmail;

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            var peopleCount = 3;
            ServiceManager = MockServiceManager.SetPeopleToMultiple(peopleCount);

            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(FoundMultiContactResponse(testRecipient))
                .AssertReply(ShowContactsList(Strings.Strings.DefaultUserName))
                .Send(CreateMeetingTestUtterances.ChooseFirstUser)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(testDupRecipient, testDupEmailAddress))
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt(string.Format(Strings.Strings.UserName, 0), testDupEmailAddress))
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithWrongContactName()
        {
            var peopleCount = 3;
            ServiceManager = MockServiceManager.SetPeopleToMultiple(peopleCount);
            var testRecipient = string.Format(Strings.Strings.UserName, 0);
            var testEmailAddress = string.Format(Strings.Strings.UserEmailAddress, 0);

            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send("wrong name")
                .AssertReplyOneOf(UserNotFoundPrompt("wrong name"))
                .AssertReplyOneOf(AskForEmailPrompt())
                .Send("wrong name")
                .AssertReplyOneOf(UserNotFoundAgainPrompt("wrong name"))
                .Send(string.Format(Strings.Strings.UserName, 0))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(testRecipient, testEmailAddress))
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt(testRecipient, testEmailAddress))
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithOneContactMultipleEmails()
        {
            ServiceManager = MockServiceManager.SetOnePeopleEmailsToMultiple(3);

            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(FoundMultiEmailResponse(Strings.Strings.DefaultUserName))
                .AssertReply(ShowEmailsList(Strings.Strings.DefaultUserName))
                .Send(CreateMeetingTestUtterances.ChooseFirstUser)
                .AssertReplyOneOf(EmailChoiceConfirmationResponse(string.Format(Strings.Strings.UserEmailAddress, 0)))
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt(Strings.Strings.DefaultUserName, string.Format(Strings.Strings.UserEmailAddress, 0)))
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithTitleEntity()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.CreateMeetingWithTitleEntity)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithOneContactEntity()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithDateTimeEntity()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.CreateMeetingWithDateTimeEntity)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(CheckCreatedMeetingInFuture())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithLocationEntity()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.CreateMeetingWithLocationEntity)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithDurationEntity()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.CreateMeetingWithDurationEntity)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWeekday()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserEmail)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(Strings.Strings.DefaultUserEmail))
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithEmailAddressPrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.WeekdayDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(CheckCreatedMeetingInFuture())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarAccessDeniedException()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.ThrowErrorAccessDenied)
                .AssertReplyOneOf(BotErrorResponse())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateShowRestParticipants()
        {
            this.ServiceManager = MockServiceManager.SetPeopleToMultiple(6);

            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(string.Format(Strings.Strings.UserName, 0))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(string.Format(Strings.Strings.UserName, 0), string.Format(Strings.Strings.UserEmailAddress, 0)))
                .AssertReplyOneOf(AddMoreUserPromptWithMultipleUsers(1))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AskForAddMoreAttendeesPrompt())
                .Send(string.Format(Strings.Strings.UserName, 1))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(string.Format(Strings.Strings.UserName, 1), string.Format(Strings.Strings.UserEmailAddress, 1)))
                .AssertReplyOneOf(AddMoreUserPromptWithMultipleUsers(2))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AskForAddMoreAttendeesPrompt())
                .Send(string.Format(Strings.Strings.UserName, 2))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(string.Format(Strings.Strings.UserName, 2), string.Format(Strings.Strings.UserEmailAddress, 2)))
                .AssertReplyOneOf(AddMoreUserPromptWithMultipleUsers(3))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AskForAddMoreAttendeesPrompt())
                .Send(string.Format(Strings.Strings.UserName, 3))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(string.Format(Strings.Strings.UserName, 3), string.Format(Strings.Strings.UserEmailAddress, 3)))
                .AssertReplyOneOf(AddMoreUserPromptWithMultipleUsers(4))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AskForAddMoreAttendeesPrompt())
                .Send(string.Format(Strings.Strings.UserName, 4))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(string.Format(Strings.Strings.UserName, 4), string.Format(Strings.Strings.UserEmailAddress, 4)))
                .AssertReplyOneOf(AddMoreUserPromptWithMultipleUsers(5))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AskForAddMoreAttendeesPrompt())
                .Send(string.Format(Strings.Strings.UserName, 5))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(string.Format(Strings.Strings.UserName, 5), string.Format(Strings.Strings.UserEmailAddress, 5)))
                .AssertReplyOneOf(AddMoreUserPromptWithMultipleUsers(6))
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithMultipleContactNamePrompt(6))
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(AskForShowRestParticipantsPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(RestParticipantsResponse(6))
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateRetryDateTooMany()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send("test")
                .AssertReplyOneOf(AskForDateReprompt())
                .Send("test")
                .AssertReplyOneOf(AskForDateReprompt())
                .Send("test")
                .AssertReplyOneOf(AskForDateReprompt())
                .Send("test")
                .AssertReplyOneOf(RetryTooManyResponse())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateRetryTimeTooMany()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send("test")
                .AssertReplyOneOf(AskForStartTimeReprompt())
                .Send("test")
                .AssertReplyOneOf(AskForStartTimeReprompt())
                .Send("test")
                .AssertReplyOneOf(AskForStartTimeReprompt())
                .Send("test")
                .AssertReplyOneOf(RetryTooManyResponse())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateRetryDurationTooMany()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send("test")
                .AssertReplyOneOf(AskForDurationReprompt())
                .Send("test")
                .AssertReplyOneOf(AskForDurationReprompt())
                .Send("test")
                .AssertReplyOneOf(AskForDurationReprompt())
                .Send("test")
                .AssertReplyOneOf(RetryTooManyResponse())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmBookMeetingRoom()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(GetTemplates(CalendarMainResponses.CalendarWelcomeMessage))
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress())
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(AskForMeetingRoomPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AskForBuildingPrompt())
                .Send(Strings.Strings.DefaultBuilding)
                .AssertReplyOneOf(AskForFloorNumberPrompt())
                .Send(Strings.Strings.DefaultFloorNumber)
                .AssertReplyOneOf(AskForConfirmMeetingRoomPrompt(dateTime: "at 9:00 AM"))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(BookedMeeting())
                .StartTestAsync();
        }

        private string[] ConfirmOneNameOneAddress(string address)
        {
            return GetTemplates(FindContactResponses.PromptOneNameOneAddress, new
            {
                User = address
            });
        }

        private string[] ConfirmOneNameOneAddress(string name = Strings.Strings.DefaultUserName, string address = Strings.Strings.DefaultUserEmail)
        {
            return GetTemplates(FindContactResponses.PromptOneNameOneAddress, new
            {
                User = $"{name} ({address})"
            });
        }

        private string[] AddMoreUserPromptWithMultipleUsers(int count)
        {
            var resultString = string.Empty;
            for (int i = 0; i < count; i++)
            {
                if (i != 0)
                {
                    if (i == count - 1)
                    {
                        resultString += " and ";
                    }
                    else
                    {
                        resultString += ", ";
                    }
                }

                resultString += $"{string.Format(Strings.Strings.UserName, i)}";
            }

            return GetTemplates(FindContactResponses.AddMoreUserPrompt, new
            {
                Users = resultString
            });
        }

        private string[] AddMoreUserPrompt()
        {
            return GetTemplates(FindContactResponses.AddMoreUserPrompt);
        }

        private string[] AskForParticpantsPrompt()
        {
            return GetTemplates(FindContactResponses.NoAttendees);
        }

        private string[] AskForSubjectWithEmailAddressPrompt()
        {
            return GetTemplates(CreateEventResponses.NoTitle, new
            {
                UserName = Strings.Strings.DefaultUserEmail
            });
        }

        private string[] AskForSubjectWithContactNamePrompt(string userName = null, string userEmail = null)
        {
            return GetTemplates(CreateEventResponses.NoTitle, new
            {
                UserName = userName ?? Strings.Strings.DefaultUserName
            });
        }

        private string[] AskForSubjectWithMultipleContactNamePrompt(int count)
        {
            var resultString = string.Empty;
            for (int i = 0; i < count; i++)
            {
                if (i != 0)
                {
                    if (i == count - 1)
                    {
                        resultString += " and ";
                    }
                    else
                    {
                        resultString += ", ";
                    }
                }

                resultString += $"{string.Format(Strings.Strings.UserName, i)}";
            }

            return GetTemplates(CreateEventResponses.NoTitle, new
            {
                UserName = resultString
            });
        }

        private string[] AskForSubjectShortPrompt(string userName = null)
        {
            return GetTemplates(CreateEventResponses.NoTitleShort);
        }

        private string[] AskForContentPrompt()
        {
            return GetTemplates(CreateEventResponses.NoContent);
        }

        private string[] AskForDatePrompt()
        {
            return GetTemplates(CreateEventResponses.NoStartDate);
        }

        private string[] AskForDateReprompt()
        {
            return GetTemplates(CreateEventResponses.NoStartDateRetry);
        }

        private string[] AskForStartTimePrompt()
        {
            return GetTemplates(CreateEventResponses.NoStartTime);
        }

        private string[] AskForStartTimeReprompt()
        {
            return GetTemplates(CreateEventResponses.NoStartTimeRetry);
        }

        private string[] AskForDurationPrompt()
        {
            return GetTemplates(CreateEventResponses.NoDuration);
        }

        private string[] AskForDurationReprompt()
        {
            return GetTemplates(CreateEventResponses.NoDurationRetry);
        }

        private string[] AskForMeetingRoomPrompt()
        {
            return GetTemplates(CreateEventResponses.NoMeetingRoom);
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
            return GetTemplates(FindMeetingRoomResponses.ConfirmMeetingRoomPrompt, new
            {
                MeetingRoom = string.Format(Strings.Strings.MeetingRoomName, roomNumber),
                DateTime = dateTime
            });
        }

        private string[] AskForLocationPrompt()
        {
            return GetTemplates(CreateEventResponses.NoLocation);
        }

        private string[] AskForRecreateInfoPrompt()
        {
            return GetTemplates(CreateEventResponses.GetRecreateInfo);
        }

        private string[] AskForRecreateInfoReprompt()
        {
            return GetTemplates(CreateEventResponses.GetRecreateInfoRetry);
        }

        private string[] ConfirmedMeetingRoom()
        {
            return GetTemplates(FindMeetingRoomResponses.ConfirmedMeetingRoom);
        }

        private string[] RetryTooManyResponse()
        {
            return GetTemplates(CalendarSharedResponses.RetryTooManyResponse);
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

        private Action<IActivity> CheckCreatedMeetingInFuture()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private Action<IActivity> ShowContactsList(string ContactName)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = GetTemplates(
                    FindContactResponses.ConfirmMultipleContactNameSinglePage,
                    new
                    {
                        ContactName = ContactName
                    });

                var messageLines = messageActivity.Text.Split("\r\n");
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
                Assert.IsTrue(messageLines.Length == 8);
            };
        }

        private Action<IActivity> ShowEmailsList(string userName)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var confirmedMessage = new List<string>(
                   GetTemplates(FindContactResponses.ConfirmMultipleContactEmailSinglePage));

                var messageLines = messageActivity.Text.Split("\r\n");
                Assert.IsTrue(confirmedMessage.Contains(messageLines[0]));
                Assert.IsTrue(messageLines.Length == 5);
            };
        }

        private string[] BotErrorResponse()
        {
            return GetTemplates(CalendarSharedResponses.CalendarErrorMessageAccountProblem);
        }

        private string[] AskForAddMoreAttendeesPrompt()
        {
            return GetTemplates(FindContactResponses.AddMoreAttendees);
        }

        private string[] AskForShowRestParticipantsPrompt()
        {
            return GetTemplates(CreateEventResponses.ShowRestParticipantsPrompt);
        }

        private string RestParticipantsResponse(int count)
        {
            var resultString = string.Empty;
            for (int i = 5; i < count; i++)
            {
                if (i != 5)
                {
                    if (i == count - 1)
                    {
                        resultString += " and ";
                    }
                    else
                    {
                        resultString += ", ";
                    }
                }

                resultString += $"{string.Format(Strings.Strings.UserName, i)}";
            }

            return resultString;
        }

        private string[] UserNotFoundPrompt(string userName)
        {
            return GetTemplates(FindContactResponses.UserNotFound, new
            {
                UserName = userName
            });
        }

        private string[] UserNotFoundAgainPrompt(string userName)
        {
            return GetTemplates(FindContactResponses.UserNotFoundAgain, new
            {
                source = "Outlook",
                UserName = userName
            });
        }

        private string[] FoundMultiContactResponse(string userName)
        {
            return GetTemplates(FindContactResponses.FindMultipleContactNames, new
            {
                UserName = userName
            });
        }

        private string[] FoundMultiEmailResponse(string userName)
        {
            return GetTemplates(FindContactResponses.FindMultipleEmails, new
            {
                UserName = userName
            });
        }

        private string[] EmailChoiceConfirmationResponse(string email)
        {
            return GetTemplates(FindContactResponses.EmailChoiceConfirmation, new
            {
                Email = email
            });
        }

        private string[] AskForEmailPrompt()
        {
            return GetTemplates(FindContactResponses.AskForEmail);
        }
    }
}
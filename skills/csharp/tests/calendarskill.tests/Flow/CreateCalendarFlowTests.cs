// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.FindContact;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Test.Flow.Fakes;
using CalendarSkill.Test.Flow.Utterances;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkill.Test.Flow
{
    [TestClass]
    public class CreateCalendarFlowTests : CalendarSkillTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var botServices = Services.BuildServiceProvider().GetService<BotServices>();
            botServices.CognitiveModelSets.Add("en", new CognitiveModelSet()
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
            var testRecipient = Strings.Strings.DefaultUserName;
            var testEmailAddress = Strings.Strings.DefaultUserEmail;

            var recipientDict = new StringDictionary() { { "User", $"{testRecipient}: {testEmailAddress}" } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
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
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeTime()
        {
            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneContactPrompt())
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
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeDuration()
        {
            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneContactPrompt())
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
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeLocation()
        {
            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneContactPrompt())
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
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithLocation)

                // test limitation for now. need to do further investigate.
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeParticipants()
        {
            var testRecipient = Strings.Strings.DefaultUserName;
            var testEmailAddress = Strings.Strings.DefaultUserEmail;

            var recipientDict = new StringDictionary() { { "User", $"{testRecipient}: {testEmailAddress}" } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneContactPrompt())
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
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeSubject()
        {
            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneContactPrompt())
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
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeContent()
        {
            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneContactPrompt())
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
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithEmailAddress()
        {
            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserEmail)
                .AssertReplyOneOf(AddMoreUserPrompt(Strings.Strings.DefaultUserEmail))
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
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
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
            var recipientDupDict = new StringDictionary() { { "User", $"{testDupRecipient}: {testDupEmailAddress}" } };

            var peopleCount = 3;
            ServiceManager = MockServiceManager.SetPeopleToMultiple(peopleCount);

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReply(ShowContactsList(recipientDict))
                .Send(CreateMeetingTestUtterances.ChooseFirstUser)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDupDict))
                .AssertReplyOneOf(AddMoreUserPrompt(testDupRecipient, testDupEmailAddress))
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
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithWrongContactName()
        {
            var peopleCount = 3;
            ServiceManager = MockServiceManager.SetPeopleToMultiple(peopleCount);
            var testRecipient = string.Format(Strings.Strings.UserName, 0);
            var testEmailAddress = string.Format(Strings.Strings.UserEmailAddress, 0);
            var recipientDict = new StringDictionary() { { "User", $"{testRecipient}: {testEmailAddress}" } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send("wrong name")
                .AssertReplyOneOf(UserNotFoundPrompt("wrong name"))
                .Send("wrong name")
                .AssertReplyOneOf(UserNotFoundAgainPrompt("wrong name"))
                .Send(string.Format(Strings.Strings.UserName, 0))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .AssertReplyOneOf(AddMoreUserPrompt(testRecipient, testEmailAddress))
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
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithOneContactMultipleEmails()
        {
            ServiceManager = MockServiceManager.SetOnePeopleEmailsToMultiple(3);

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReply(ShowEmailsList(Strings.Strings.DefaultUserName))
                .Send(CreateMeetingTestUtterances.ChooseFirstUser)
                .AssertReplyOneOf(AddMoreUserPrompt(Strings.Strings.DefaultUserName, string.Format(Strings.Strings.UserEmailAddress, 0)))
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
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithTitleEntity()
        {
            var testRecipient = Strings.Strings.DefaultUserName;
            var testEmailAddress = Strings.Strings.DefaultUserEmail;
            var recipientDict = new StringDictionary() { { "User", $"{testRecipient}: {testEmailAddress}" } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithTitleEntity)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
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
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithOneContactEntity()
        {
            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReplyOneOf(ConfirmOneContactPrompt())
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
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithDateTimeEntity()
        {
            var testRecipient = Strings.Strings.DefaultUserName;
            var testEmailAddress = Strings.Strings.DefaultUserEmail;
            var recipientDict = new StringDictionary() { { "User", $"{testRecipient}: {testEmailAddress}" } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithDateTimeEntity)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(CheckCreatedMeetingInFuture())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithLocationEntity()
        {
            var testRecipient = Strings.Strings.DefaultUserName;
            var testEmailAddress = Strings.Strings.DefaultUserEmail;
            var recipientDict = new StringDictionary() { { "User", $"{testRecipient}: {testEmailAddress}" } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithLocationEntity)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
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
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithDurationEntity()
        {
            var testRecipient = Strings.Strings.DefaultUserName;
            var testEmailAddress = Strings.Strings.DefaultUserEmail;
            var recipientDict = new StringDictionary() { { "User", $"{testRecipient}: {testEmailAddress}" } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithDurationEntity)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
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
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWeekday()
        {
            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserEmail)
                .AssertReplyOneOf(AddMoreUserPrompt(Strings.Strings.DefaultUserEmail))
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
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(CheckCreatedMeetingInFuture())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarAccessDeniedException()
        {
            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.ThrowErrorAccessDenied)
                .AssertReplyOneOf(BotErrorResponse())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateShowRestParticipants()
        {
            this.ServiceManager = MockServiceManager.SetPeopleToMultiple(6);

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(string.Format(Strings.Strings.UserName, 0))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(new StringDictionary()
                {
                    { "User", $"{string.Format(Strings.Strings.UserName, 0)}: {string.Format(Strings.Strings.UserEmailAddress, 0)}" }
                }))
                .AssertReplyOneOf(AddMoreUserPromptWithMultipleUsers(1))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AskForAddMoreAttendeesPrompt())
                .Send(string.Format(Strings.Strings.UserName, 1))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(new StringDictionary()
                {
                    { "User", $"{string.Format(Strings.Strings.UserName, 1)}: {string.Format(Strings.Strings.UserEmailAddress, 1)}" }
                }))
                .AssertReplyOneOf(AddMoreUserPromptWithMultipleUsers(2))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AskForAddMoreAttendeesPrompt())
                .Send(string.Format(Strings.Strings.UserName, 2))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(new StringDictionary()
                {
                    { "User", $"{string.Format(Strings.Strings.UserName, 2)}: {string.Format(Strings.Strings.UserEmailAddress, 2)}" }
                }))
                .AssertReplyOneOf(AddMoreUserPromptWithMultipleUsers(3))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AskForAddMoreAttendeesPrompt())
                .Send(string.Format(Strings.Strings.UserName, 3))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(new StringDictionary()
                {
                    { "User", $"{string.Format(Strings.Strings.UserName, 3)}: {string.Format(Strings.Strings.UserEmailAddress, 3)}" }
                }))
                .AssertReplyOneOf(AddMoreUserPromptWithMultipleUsers(4))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AskForAddMoreAttendeesPrompt())
                .Send(string.Format(Strings.Strings.UserName, 4))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(new StringDictionary()
                {
                    { "User", $"{string.Format(Strings.Strings.UserName, 4)}: {string.Format(Strings.Strings.UserEmailAddress, 4)}" }
                }))
                .AssertReplyOneOf(AddMoreUserPromptWithMultipleUsers(5))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AskForAddMoreAttendeesPrompt())
                .Send(string.Format(Strings.Strings.UserName, 5))
                .AssertReplyOneOf(ConfirmOneNameOneAddress(new StringDictionary()
                {
                    { "User", $"{string.Format(Strings.Strings.UserName, 5)}: {string.Format(Strings.Strings.UserEmailAddress, 5)}" }
                }))
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
                .AssertReplyOneOf(AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(AskForShowRestParticipantsPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(RestParticipantsResponse(6))
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        private string[] ConfirmOneNameOneAddress(StringDictionary recipientDict)
        {
            return ParseReplies(FindContactResponses.PromptOneNameOneAddress, recipientDict);
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

            var responseParams = new StringDictionary()
            {
                { "Users", resultString }
            };
            return ParseReplies(FindContactResponses.AddMoreUserPrompt, responseParams);
        }

        private string[] AddMoreUserPrompt(string userName = null, string userEmail = null)
        {
            var responseParams = new StringDictionary()
            {
                { "Users", $"{userName ?? Strings.Strings.DefaultUserName}" }
            };
            return ParseReplies(FindContactResponses.AddMoreUserPrompt, responseParams);
        }

        private string[] AskForParticpantsPrompt()
        {
            return ParseReplies(CreateEventResponses.NoAttendees, new StringDictionary());
        }

        private string[] AskForSubjectWithEmailAddressPrompt()
        {
            var responseParams = new StringDictionary()
            {
                { "UserName", $"{Strings.Strings.DefaultUserEmail}" },
            };

            return ParseReplies(CreateEventResponses.NoTitle, responseParams);
        }

        private string[] AskForSubjectWithContactNamePrompt(string userName = null, string userEmail = null)
        {
            var responseParams = new StringDictionary()
            {
                { "UserName", $"{userName ?? Strings.Strings.DefaultUserName}" },
            };

            return ParseReplies(CreateEventResponses.NoTitle, responseParams);
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

            var responseParams = new StringDictionary()
            {
                { "UserName", resultString }
            };

            return ParseReplies(CreateEventResponses.NoTitle, responseParams);
        }

        private string[] ConfirmOneContactPrompt(string userName = null, string emailAddress = null)
        {
            var responseParams = new StringDictionary()
            {
                { "User", $"{userName ?? Strings.Strings.DefaultUserName}: {emailAddress ?? Strings.Strings.DefaultUserEmail}" }
            };

            return ParseReplies(FindContactResponses.PromptOneNameOneAddress, responseParams);
        }

        private string[] AskForSubjectShortPrompt(string userName = null)
        {
            return ParseReplies(CreateEventResponses.NoTitleShort, new StringDictionary());
        }

        private string[] AskForContentPrompt()
        {
            return ParseReplies(CreateEventResponses.NoContent, new StringDictionary());
        }

        private string[] AskForDatePrompt()
        {
            return ParseReplies(CreateEventResponses.NoStartDate, new StringDictionary());
        }

        private string[] AskForStartTimePrompt()
        {
            return ParseReplies(CreateEventResponses.NoStartTime, new StringDictionary());
        }

        private string[] AskForDurationPrompt()
        {
            return ParseReplies(CreateEventResponses.NoDuration, new StringDictionary());
        }

        private string[] AskForLocationPrompt()
        {
            return ParseReplies(CreateEventResponses.NoLocation, new StringDictionary());
        }

        private string[] AskForRecreateInfoPrompt()
        {
            return ParseReplies(CreateEventResponses.GetRecreateInfo, new StringDictionary());
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
            return ParseReplies(CreateEventResponses.ConfirmCreatePrompt, new StringDictionary());
        }

        private Action<IActivity> CheckCreatedMeetingInFuture()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private Action<IActivity> ShowContactsList(StringDictionary recipientDict)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = ParseReplies(FindContactResponses.ConfirmMultipleContactNameSinglePage, recipientDict);

                var messageLines = messageActivity.Text.Split("\r\n");
                Assert.IsTrue(messageActivity.Text.StartsWith(recipientConfirmedMessage[0]));
                Assert.IsTrue(messageLines.Length == 5);
            };
        }

        private Action<IActivity> ShowEmailsList(string userName)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                var responseParams = new StringDictionary()
                {
                    { "UserName", userName }
                };
                var confirmedMessage = new List<string>(ParseReplies(FindContactResponses.ConfirmMultiplContactEmailSinglePage, responseParams));

                var messageLines = messageActivity.Text.Split("\r\n");
                Assert.IsTrue(confirmedMessage.Contains(messageLines[0]));
                Assert.IsTrue(messageLines.Length == 5);
            };
        }

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.Handoff);
            };
        }

        private string[] BotErrorResponse()
        {
            return ParseReplies(CalendarSharedResponses.CalendarErrorMessageAccountProblem, new StringDictionary());
        }

        private string[] AskForAddMoreAttendeesPrompt()
        {
            return ParseReplies(FindContactResponses.AddMoreAttendees, new StringDictionary());
        }

        private string[] AskForShowRestParticipantsPrompt()
        {
            return ParseReplies(CreateEventResponses.ShowRestParticipantsPrompt, new StringDictionary());
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
            return ParseReplies(FindContactResponses.UserNotFound, new StringDictionary() { { "UserName", userName } });
        }

        private string[] UserNotFoundAgainPrompt(string userName)
        {
            return ParseReplies(
                FindContactResponses.UserNotFoundAgain,
                new StringDictionary()
                {
                    { "source", "Outlook" },
                    { "UserName", userName }
                });
        }
    }
}
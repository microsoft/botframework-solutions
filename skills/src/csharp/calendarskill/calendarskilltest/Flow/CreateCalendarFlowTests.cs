using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.FindContact;
using CalendarSkill.Responses.Shared;
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
    public class CreateCalendarFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var botServices = Services.BuildServiceProvider().GetService<BotServices>();
            botServices.CognitiveModelSets.Add("en", new CognitiveModelSet()
            {
                LuisServices = new Dictionary<string, ITelemetryRecognizer>()
                {
                    { "general", new MockLuisRecognizer() },
                    { "calendar", new MockLuisRecognizer(new CreateMeetingTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarCreate()
        {
            var testRecipient = Strings.Strings.DefaultUserName;
            var testEmailAddress = Strings.Strings.DefaultUserEmail;

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .Send(Strings.Strings.ConfirmYes)
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
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithTime)

                // test limitation for now. need to do further investigate.
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
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
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithDuration)

                // test limitation for now. need to do further investigate.
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReply(ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(ConfirmPrompt())
                .AssertReply(ShowCalendarList())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeLocation()
        {
            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithLocation)

                // test limitation for now. need to do further investigate.
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
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

            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithParticipants)

                // test limitation for now. need to do further investigate.
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .Send(Strings.Strings.ConfirmYes)
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
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithSubject)

                // test limitation for now. need to do further investigate.
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
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
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(ShowCalendarList())
                .AssertReplyOneOf(ConfirmPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithContent)

                // test limitation for now. need to do further investigate.
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
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
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
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
            var recipientDupDict = new StringDictionary() { { "UserName", testDupRecipient }, { "EmailAddress", testDupEmailAddress } };

            var peopleCount = 3;
            ServiceManager = MockServiceManager.SetPeopleToMultiple(peopleCount);

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReply(ShowContactsList(recipientDict))
                .Send(CreateMeetingTestUtterances.ChooseFirstUser)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDupDict))
                .Send(Strings.Strings.ConfirmYes)
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
        public async Task Test_CalendarCreateWithTitleEntity()
        {
            var testRecipient = Strings.Strings.DefaultUserName;
            var testEmailAddress = Strings.Strings.DefaultUserEmail;
            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithTitleEntity)
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
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
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
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
            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithDateTimeEntity)
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
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
            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithLocationEntity)
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
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
            var recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithDurationEntity)
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(ConfirmOneNameOneAddress(recipientDict))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(AddMoreUserPrompt())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
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
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
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
                .AssertReply(ShowAuth())
                .Send(GetAuthResponse())
                .AssertReplyOneOf(AskForParticpantsPrompt())
                .Send(Strings.Strings.ThrowErrorAccessDenied)
                .AssertReplyOneOf(BotErrorResponse())
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        private string[] ConfirmOneNameOneAddress(StringDictionary recipientDict)
        {
            return ParseReplies(FindContactResponses.PromptOneNameOneAddress, recipientDict);
        }

        private string[] AddMoreUserPrompt(string userName = null, string userEmail = null)
        {
            var responseParams = new StringDictionary()
            {
                { "Users", $"{userName ?? Strings.Strings.DefaultUserName}: {userEmail ?? Strings.Strings.DefaultUserEmail}" }
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
                { "UserName", $"{Strings.Strings.DefaultUserEmail}: {Strings.Strings.DefaultUserEmail}" },
            };

            return ParseReplies(CreateEventResponses.NoTitle, responseParams);
        }

        private string[] AskForSubjectWithContactNamePrompt(string userName = null, string userEmail = null)
        {
            var responseParams = new StringDictionary()
            {
                { "UserName", $"{userName ?? Strings.Strings.DefaultUserName}: {userEmail ?? Strings.Strings.DefaultUserEmail}" },
            };

            return ParseReplies(CreateEventResponses.NoTitle, responseParams);
        }

        private string[] ConfirmOneContactPrompt(string userName = null, string emailAddress = null)
        {
            var responseParams = new StringDictionary()
            {
                { "UserName", userName ?? Strings.Strings.DefaultUserName },
                { "EmailAddress", emailAddress ?? Strings.Strings.DefaultUserEmail }
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

        private Action<IActivity> ActionEndMessage()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation);
            };
        }

        private string[] BotErrorResponse()
        {
            return ParseReplies(CalendarSharedResponses.CalendarErrorMessageBotProblem, new StringDictionary());
        }
    }
}
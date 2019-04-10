using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using CalendarSkill.Dialogs.FindContact.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkillTest.Flow.Fakes;
using CalendarSkillTest.Flow.Models;
using CalendarSkillTest.Flow.Utterances;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class CreateCalendarFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            this.Services.LocaleConfigurations.Add("en", new LocaleConfiguration()
            {
                Locale = "en-us",
                LuisServices = new Dictionary<string, ITelemetryLuisRecognizer>()
                {
                    { "general", new MockLuisRecognizer() },
                    { "calendar", new MockLuisRecognizer(new CreateMeetingTestUtterances()) }
                }
            });
        }

        [TestMethod]
        public async Task Test_CalendarCreate()
        {
            string testRecipient = Strings.Strings.DefaultUserName;
            string testEmailAddress = Strings.Strings.DefaultUserEmail;

            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.AskForSubjectWithContactNamePrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(this.AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(this.AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(this.AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(this.AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeTime()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(this.AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithTime)

                // test limitation for now. need to do further investigate.
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeDuration()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(this.AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithDuration)

                // test limitation for now. need to do further investigate.
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeLocation()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(this.AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithLocation)

                // test limitation for now. need to do further investigate.
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeParticipants()
        {
            string testRecipient = Strings.Strings.DefaultUserName;
            string testEmailAddress = Strings.Strings.DefaultUserEmail;

            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };

            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(this.AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithParticipants)

                // test limitation for now. need to do further investigate.
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDict))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeSubject()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(this.AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithSubject)

                // test limitation for now. need to do further investigate.
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForSubjectShortPrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreate_ConfirmNo_ChangeContent()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmNo)
                .AssertReplyOneOf(this.AskForRecreateInfoPrompt())
                .Send(Strings.Strings.RecreateWithContent)

                // test limitation for now. need to do further investigate.
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithEmailAddress()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserEmail)
                .AssertReplyOneOf(this.AskForSubjectWithEmailAddressPrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(this.AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(this.AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(this.AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(this.AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithMultipleContacts()
        {
            string testDupRecipient = string.Format(Strings.Strings.UserName, 0);
            string testDupEmailAddress = string.Format(Strings.Strings.UserEmailAddress, 0);
            string testRecipient = Strings.Strings.DefaultUserName;
            string testEmailAddress = Strings.Strings.DefaultUserEmail;

            StringDictionary recipientDict = new StringDictionary() { { "UserName", testRecipient }, { "EmailAddress", testEmailAddress } };
            StringDictionary recipientDupDict = new StringDictionary() { { "UserName", testDupRecipient }, { "EmailAddress", testDupEmailAddress } };

            int peopleCount = 3;
            this.ServiceManager = MockServiceManager.SetPeopleToMultiple(peopleCount);

            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReply(this.ShowContactsList(recipientDict))
                .Send(CreateMeetingTestUtterances.ChooseFirstUser)
                .AssertReplyOneOf(this.ConfirmOneNameOneAddress(recipientDupDict))
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.AskForSubjectWithContactNamePrompt(string.Format(Strings.Strings.UserName, 0)))
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(this.AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(this.AskForDatePrompt())
                .Send(Strings.Strings.DefaultStartDate)
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(this.AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(this.AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithTitleEntity()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithTitleEntity)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithOneContactEntity()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.ConfirmOneContactPrompt())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithDateTimeEntity()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithDateTimeEntity)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReply(this.CheckCreatedMeetingInFuture())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithLocationEntity()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithLocationEntity)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWithDurationEntity()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithDurationEntity)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReply(this.ShowCalendarList())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarCreateWeekday()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserEmail)
                .AssertReplyOneOf(this.AskForSubjectWithEmailAddressPrompt())
                .Send(Strings.Strings.DefaultEventName)
                .AssertReplyOneOf(this.AskForContentPrompt())
                .Send(Strings.Strings.DefaultContent)
                .AssertReplyOneOf(this.AskForDatePrompt())
                .Send(Strings.Strings.WeekdayDate)
                .AssertReplyOneOf(this.AskForStartTimePrompt())
                .Send(Strings.Strings.DefaultStartTime)
                .AssertReplyOneOf(this.AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
                .AssertReplyOneOf(this.AskForLocationPrompt())
                .Send(Strings.Strings.DefaultLocation)
                .AssertReply(this.CheckCreatedMeetingInFuture())
                .Send(Strings.Strings.ConfirmYes)
                .AssertReply(this.ShowCalendarList())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_CalendarAccessDeniedException()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForParticpantsPrompt())
                .Send(Strings.Strings.ThrowErrorAccessDenied)
                .AssertReplyOneOf(this.BotErrorResponse())
                .AssertReply(this.ActionEndMessage())
                .StartTestAsync();
        }

        private string[] ConfirmOneNameOneAddress(StringDictionary recipientDict)
        {
            return this.ParseReplies(FindContactResponses.PromptOneNameOneAddress, recipientDict);
        }

        private string[] AskForParticpantsPrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoAttendees, new StringDictionary());
        }

        private string[] AskForSubjectWithEmailAddressPrompt()
        {
            var responseParams = new StringDictionary()
            {
                { "UserName", Strings.Strings.DefaultUserEmail },
            };

            return this.ParseReplies(CreateEventResponses.NoTitle, responseParams);
        }

        private string[] AskForSubjectWithContactNamePrompt(string userName = null)
        {
            var responseParams = new StringDictionary()
            {
                { "UserName", userName ?? Strings.Strings.DefaultUserName },
            };

            return this.ParseReplies(CreateEventResponses.NoTitle, responseParams);
        }

        private string[] ConfirmOneContactPrompt(string userName = null, string emailAddress = null)
        {
            var responseParams = new StringDictionary()
            {
                { "UserName", userName ?? Strings.Strings.DefaultUserName },
                { "EmailAddress", emailAddress ?? Strings.Strings.DefaultUserEmail }
            };

            return this.ParseReplies(FindContactResponses.PromptOneNameOneAddress, responseParams);
        }

        private string[] AskForSubjectShortPrompt(string userName = null)
        {
            return this.ParseReplies(CreateEventResponses.NoTitleShort, new StringDictionary());
        }

        private string[] AskForContentPrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoContent, new StringDictionary());
        }

        private string[] AskForDatePrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoStartDate, new StringDictionary());
        }

        private string[] AskForStartTimePrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoStartTime, new StringDictionary());
        }

        private string[] AskForDurationPrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoDuration, new StringDictionary());
        }

        private string[] AskForLocationPrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoLocation, new StringDictionary());
        }

        private string[] AskForRecreateInfoPrompt()
        {
            return this.ParseReplies(CreateEventResponses.GetRecreateInfo, new StringDictionary());
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

        private Action<IActivity> CheckCreatedMeetingInFuture()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);

                //var meetingCardJsonString = ((Newtonsoft.Json.Linq.JObject)messageActivity.Attachments[0].Content).ToString();
                //var meetingCard = JsonConvert.DeserializeObject<MeetingAdaptiveCard>(meetingCardJsonString);
                //var meetingDate = meetingCard.Bodies[0].Items[2].Text;
                //var cultureInfo = (CultureInfo)CultureInfo.CurrentUICulture.Clone();
                //cultureInfo.DateTimeFormat.DateSeparator = "-";
                //var date = DateTime.ParseExact(meetingDate, "d", cultureInfo);
                //var utcToday = DateTime.UtcNow.Date;
                //Assert.IsTrue(date >= utcToday);
            };
        }

        private Action<IActivity> ShowContactsList(StringDictionary recipientDict)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(FindContactResponses.ConfirmMultipleContactNameSinglePage, recipientDict);

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
            return this.ParseReplies(CalendarSharedResponses.CalendarErrorMessageBotProblem, new StringDictionary());
        }
    }
}
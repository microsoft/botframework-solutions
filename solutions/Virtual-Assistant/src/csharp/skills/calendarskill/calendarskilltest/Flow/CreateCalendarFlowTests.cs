using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.CreateEvent.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkillTest.Flow.Fakes;
using CalendarSkillTest.Flow.Models;
using CalendarSkillTest.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Skills;
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

            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupCalendarService(MockCalendarService.FakeDefaultEvents());
            serviceManager.SetupUserService(MockUserService.FakeDefaultUsers(), MockUserService.FakeDefaultPeople());
        }

        [TestMethod]
        public async Task Test_CalendarCreate()
        {
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
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
                .AssertReplyOneOf(this.AskForDurationPrompt())
                .Send(Strings.Strings.DefaultDuration)
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
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.CreateMeetingWithOneContactEntity)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
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
            int userCount = 1;
            int peopleCount = 3;
            var serviceManager = this.ServiceManager as MockCalendarServiceManager;
            serviceManager.SetupUserService(MockUserService.FakeDefaultUsers(), MockUserService.FakeMultiplePeoples(peopleCount));
            await this.GetTestFlow()
                .Send(CreateMeetingTestUtterances.BaseCreateMeeting)
                .AssertReply(this.ShowAuth())
                .Send(this.GetAuthResponse())
                .AssertReplyOneOf(this.AskForParticpantsPrompt())
                .Send(Strings.Strings.DefaultUserName)
                .AssertReply(this.ShowContactsList(userCount, peopleCount))
                .Send(CreateMeetingTestUtterances.ChooseFirstUser)
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

        private string[] AskForParticpantsPrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoAttendees.Replies, new StringDictionary());
        }

        private string[] AskForSubjectWithEmailAddressPrompt()
        {
            var responseParams = new StringDictionary()
            {
                { "UserName", Strings.Strings.DefaultUserEmail },
            };

            return this.ParseReplies(CreateEventResponses.NoTitle.Replies, responseParams);
        }

        private string[] AskForSubjectWithContactNamePrompt(string userName = null)
        {
            var responseParams = new StringDictionary()
            {
                { "UserName", userName ?? Strings.Strings.DefaultUserName },
            };

            return this.ParseReplies(CreateEventResponses.NoTitle.Replies, responseParams);
        }

        private string[] AskForSubjectShortPrompt(string userName = null)
        {
            return this.ParseReplies(CreateEventResponses.NoTitle_Short.Replies, new StringDictionary());
        }

        private string[] AskForContentPrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoContent.Replies, new StringDictionary());
        }

        private string[] AskForDatePrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoStartDate.Replies, new StringDictionary());
        }

        private string[] AskForStartTimePrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoStartTime.Replies, new StringDictionary());
        }

        private string[] AskForDurationPrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoDuration.Replies, new StringDictionary());
        }

        private string[] AskForLocationPrompt()
        {
            return this.ParseReplies(CreateEventResponses.NoLocation.Replies, new StringDictionary());
        }

        private string[] AskForRecreateInfoPrompt()
        {
            return this.ParseReplies(CreateEventResponses.GetRecreateInfo.Replies, new StringDictionary());
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

                var meetingCardJsonString = ((Newtonsoft.Json.Linq.JObject)messageActivity.Attachments[0].Content).ToString();
                var meetingCard = JsonConvert.DeserializeObject<MeetingAdaptiveCard>(meetingCardJsonString);
                var meetingDate = meetingCard.Bodies[0].Items[2].Text;
                CultureInfo cultureInfo = (CultureInfo)CultureInfo.CurrentUICulture.Clone();
                cultureInfo.DateTimeFormat.DateSeparator = "-";
                DateTime date = DateTime.ParseExact(meetingDate, "d", cultureInfo);
                DateTime utcToday = DateTime.UtcNow.Date;
                Assert.IsTrue(date >= utcToday);
            };
        }

        private Action<IActivity> ShowContactsList(int userCount, int peopleCount)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var recipientConfirmedMessage = this.ParseReplies(CreateEventResponses.ConfirmRecipient.Replies, new StringDictionary());

                var messageLines = messageActivity.Text.Split("\r\n");
                Assert.IsTrue(Array.IndexOf(recipientConfirmedMessage, messageLines[0]) != -1);
                Assert.IsTrue(messageLines.Length - 2 == userCount + peopleCount);
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
            return this.ParseReplies(CalendarSharedResponses.CalendarErrorMessageBotProblem.Replies, new StringDictionary());
        }
    }
}
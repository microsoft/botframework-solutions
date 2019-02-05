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
            var userCount = 1;
            var peopleCount = 3;
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
            var response = ResponseManager.GetResponseTemplate(CreateEventResponses.NoAttendees);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] AskForSubjectWithEmailAddressPrompt()
        {
            var responseParams = new StringDictionary()
            {
                { "UserName", Strings.Strings.DefaultUserEmail },
            };

            var response = ResponseManager.GetResponseTemplate(CreateEventResponses.NoTitle);
            return this.ParseReplies(response.Replies, responseParams);
        }

        private string[] AskForSubjectWithContactNamePrompt(string userName = null)
        {
            var responseParams = new StringDictionary()
            {
                { "UserName", userName ?? Strings.Strings.DefaultUserName },
            };

            var response = ResponseManager.GetResponseTemplate(CreateEventResponses.NoTitle);
            return this.ParseReplies(response.Replies, responseParams);
        }

        private string[] AskForSubjectShortPrompt(string userName = null)
        {
            var response = ResponseManager.GetResponseTemplate(CreateEventResponses.NoTitle_Short);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] AskForContentPrompt()
        {
            var response = ResponseManager.GetResponseTemplate(CreateEventResponses.NoContent);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] AskForDatePrompt()
        {
            var response = ResponseManager.GetResponseTemplate(CreateEventResponses.NoStartDate);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] AskForStartTimePrompt()
        {
            var response = ResponseManager.GetResponseTemplate(CreateEventResponses.NoStartTime);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] AskForDurationPrompt()
        {
            var response = ResponseManager.GetResponseTemplate(CreateEventResponses.NoDuration);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] AskForLocationPrompt()
        {
            var response = ResponseManager.GetResponseTemplate(CreateEventResponses.NoLocation);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }

        private string[] AskForRecreateInfoPrompt()
        {
            var response = ResponseManager.GetResponseTemplate(CreateEventResponses.GetRecreateInfo);
            return this.ParseReplies(response.Replies, new StringDictionary());
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
                var cultureInfo = (CultureInfo)CultureInfo.CurrentUICulture.Clone();
                cultureInfo.DateTimeFormat.DateSeparator = "-";
                var date = DateTime.ParseExact(meetingDate, "d", cultureInfo);
                var utcToday = DateTime.UtcNow.Date;
                Assert.IsTrue(date >= utcToday);
            };
        }

        private Action<IActivity> ShowContactsList(int userCount, int peopleCount)
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var response = ResponseManager.GetResponseTemplate(CreateEventResponses.ConfirmRecipient);
                var recipientConfirmedMessage = this.ParseReplies(response.Replies, new StringDictionary());

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
            var response = ResponseManager.GetResponseTemplate(CalendarSharedResponses.CalendarErrorMessageBotProblem);
            return this.ParseReplies(response.Replies, new StringDictionary());
        }
    }
}
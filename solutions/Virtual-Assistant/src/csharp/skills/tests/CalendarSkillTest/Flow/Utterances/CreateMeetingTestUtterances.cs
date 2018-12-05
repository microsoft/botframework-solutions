using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkillTest.Flow.Utterances
{
    public class CreateMeetingTestUtterances : BaseTestUtterances
    {
        public CreateMeetingTestUtterances()
        {
            this.Add(BaseCreateMeeting, GetCreateMeetingIntent(BaseCreateMeeting));
        }

        public static string BaseCreateMeeting { get; } = "Create a meeting";

        private Calendar GetCreateMeetingIntent(
            string userInput,
            Calendar.Intent intents = Calendar.Intent.CreateCalendarEntry,
            string[] subject = null,
            string[] contactName = null,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
            string[] duration = null,
            string[] meetingRoom = null,
            string[] location = null)
        {
            return GetCalendarIntent(
                userInput,
                intents,
                subject: subject,
                contactName: contactName,
                fromDate: fromDate,
                toDate: toDate,
                fromTime: fromTime,
                toTime: toTime,
                duration: duration,
                meetingRoom: meetingRoom,
                location: location);
        }
    }
}

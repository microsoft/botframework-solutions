using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkillTest.Flow.Utterances
{
    public class DeleteMeetingTestUtterances : BaseTestUtterances
    {
        public DeleteMeetingTestUtterances()
        {
            this.Add(BaseDeleteMeeting, GetBaseDeleteMeetingIntent(BaseDeleteMeeting));
        }

        public static string BaseDeleteMeeting { get; } = "delete meeting";

        private Calendar GetBaseDeleteMeetingIntent(
            string userInput,
            Calendar.Intent intents = Calendar.Intent.DeleteCalendarEntry,
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

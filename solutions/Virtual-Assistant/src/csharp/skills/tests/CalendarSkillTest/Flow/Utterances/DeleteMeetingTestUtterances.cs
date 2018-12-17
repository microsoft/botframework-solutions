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
            this.Add(DeleteMeetingWithStartTime, GetBaseDeleteMeetingIntent(
                DeleteMeetingWithStartTime,
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
            this.Add(DeleteMeetingWithTitle, GetBaseDeleteMeetingIntent(
                DeleteMeetingWithTitle,
                subject: new string[] { Strings.Strings.DefaultEventName }));
        }

        public static string BaseDeleteMeeting { get; } = "delete meeting";

        public static string DeleteMeetingWithStartTime { get; } = "delete meeting at tomorrow 6 pm";

        public static string DeleteMeetingWithTitle { get; } = $"delete {Strings.Strings.DefaultEventName} meeting";

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

using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkillTest.Flow.Utterances
{
    public class UpdateMeetingTestUtterances : BaseTestUtterances
    {
        public UpdateMeetingTestUtterances()
        {
            this.Add(BaseUpdateMeeting, GetBaseUpdateMeetingIntent(BaseUpdateMeeting));
            this.Add(UpdateMeetingWithStartTime, GetBaseUpdateMeetingIntent(
                UpdateMeetingWithStartTime,
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
            this.Add(UpdateMeetingWithTitle, GetBaseUpdateMeetingIntent(
                UpdateMeetingWithTitle,
                subject: new string[] { Strings.Strings.DefaultEventName }));
        }

        public static string BaseUpdateMeeting { get; } = "update meeting";

        public static string UpdateMeetingWithStartTime { get; } = "delete meeting at tomorrow 6 pm";

        public static string UpdateMeetingWithTitle { get; } = $"delete {Strings.Strings.DefaultEventName} meeting";

        private Calendar GetBaseUpdateMeetingIntent(
            string userInput,
            Calendar.Intent intents = Calendar.Intent.ChangeCalendarEntry,
            string[] subject = null,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null)
        {
            return GetCalendarIntent(
                userInput,
                intents,
                subject: subject,
                fromDate: fromDate,
                toDate: toDate,
                fromTime: fromTime,
                toTime: toTime);
        }
    }
}

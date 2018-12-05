using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkillTest.Flow.Utterances
{
    public class FindMeetingTestUtterances : BaseTestUtterances
    {
        public FindMeetingTestUtterances()
        {
            this.Add(BaseFindMeeting, GetBaseFindMeetingIntent(BaseFindMeeting));
            this.Add(BaseNextMeeting, GetBaseNextMeetingIntent(BaseNextMeeting));
            this.Add(BaseFindMeetingByTimeRange, GetBaseFindMeetingIntent(
                BaseFindMeetingByTimeRange,
                fromDate: new string[] { "next week" }));
        }

        public static string BaseFindMeeting { get; } = "What should I do today";

        public static string BaseFindMeetingByTimeRange { get; } = "What's on my schedule next week";

        public static string BaseNextMeeting { get; } = "what is my next meeting";

        private Calendar GetBaseFindMeetingIntent(
            string userInput,
            Calendar.Intent intents = Calendar.Intent.FindCalendarEntry,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null)
        {
            return GetCalendarIntent(
                userInput,
                intents,
                fromDate: fromDate,
                toDate: toDate,
                fromTime: fromTime,
                toTime: toTime);
        }

        private Calendar GetBaseNextMeetingIntent(string userinput, Calendar.Intent intents = Calendar.Intent.NextMeeting)
        {
            return GetCalendarIntent(userinput, intents);
        }
    }
}

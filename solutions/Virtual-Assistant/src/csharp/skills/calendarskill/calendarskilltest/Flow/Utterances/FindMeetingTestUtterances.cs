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
            this.Add(BaseNextMeeting, GetBaseFindMeetingIntent(
                BaseNextMeeting,
                orderReference: new string[] { "next" }));
            this.Add(FindMeetingByTimeRange, GetBaseFindMeetingIntent(
                FindMeetingByTimeRange,
                fromDate: new string[] { "next week" }));
            this.Add(FindMeetingByStartTime, GetBaseFindMeetingIntent(
                FindMeetingByStartTime,
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
            this.Add(HowLongNextMeetingMeeting, GetBaseFindMeetingIntent(
                HowLongNextMeetingMeeting,
                orderReference: new string[] { "next" }));
            this.Add(WhereNextMeetingMeeting, GetBaseFindMeetingIntent(
                WhereNextMeetingMeeting,
                orderReference: new string[] { "next" }));
            this.Add(WhenNextMeetingMeeting, GetBaseFindMeetingIntent(
                WhenNextMeetingMeeting,
                orderReference: new string[] { "next" }));
            this.Add(UpdateMeetingTestUtterances.BaseUpdateMeeting, UpdateMeetingTestUtterances.GetBaseUpdateMeetingIntent(UpdateMeetingTestUtterances.BaseUpdateMeeting));
        }

        public static string BaseFindMeeting { get; } = "What should I do today";

        public static string FindMeetingByTimeRange { get; } = "What's on my schedule next week";

        public static string FindMeetingByStartTime { get; } = "What are my meetings at tomorrow 6 pm";

        public static string BaseNextMeeting { get; } = "what is my next meeting";


        public static string HowLongNextMeetingMeeting { get; } = "How long is my next meeting";

        public static string WhereNextMeetingMeeting { get; } = "Where is my next meeting";

        public static string WhenNextMeetingMeeting { get; } = "When is my next meeting";

        private CalendarLU GetBaseFindMeetingIntent(
            string userInput,
            CalendarLU.Intent intents = CalendarLU.Intent.FindCalendarEntry,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
            double[] ordinal = null,
            double[] number = null,
            string[] orderReference = null,
            string[] askParameter = null)
        {
            return GetCalendarIntent(
                userInput,
                intents,
                fromDate: fromDate,
                toDate: toDate,
                fromTime: fromTime,
                toTime: toTime,
                ordinal: ordinal,
                number: number,
                orderReference: orderReference,
                askParameter: askParameter);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Luis;

namespace CalendarSkillTest.Flow.Utterances
{
    public class TimeRemainingUtterances : BaseTestUtterances
    {
        public TimeRemainingUtterances()
        {
            this.Add(NextMeetingTimeRemaining, GetBaseTimeRemainingIntent(
                NextMeetingTimeRemaining,
                orderReference: new string[] { Strings.Strings.Next }));
        }

        public static string NextMeetingTimeRemaining { get; } = $"how much time do i have before my {Strings.Strings.Next} appointment";

        private Calendar GetBaseTimeRemainingIntent(
            string userInput,
            Calendar.Intent intents = Calendar.Intent.TimeRemaining,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
            string[] orderReference = null)
        {
            return GetCalendarIntent(
                userInput,
                intents,
                fromDate: fromDate,
                toDate: toDate,
                fromTime: fromTime,
                toTime: toTime,
                orderReference: orderReference);
        }
    }
}

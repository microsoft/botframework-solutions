// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace CalendarSkill.Test.Flow.Utterances
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

        private CalendarLuis GetBaseTimeRemainingIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.TimeRemaining,
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

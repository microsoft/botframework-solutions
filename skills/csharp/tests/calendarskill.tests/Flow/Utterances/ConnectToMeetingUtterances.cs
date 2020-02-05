// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace CalendarSkill.Test.Flow.Utterances
{
    public class ConnectToMeetingUtterances : BaseTestUtterances
    {
        public ConnectToMeetingUtterances()
        {
            this.Add(BaseConnectToMeeting, GetBaseConnectToMeetingIntent(BaseConnectToMeeting));
            this.Add(JoinMeetingWithStartTime, GetBaseConnectToMeetingIntent(
                JoinMeetingWithStartTime,
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
        }

        public static string BaseConnectToMeeting { get; } = "i need to join conference call";

        public static string JoinMeetingWithStartTime { get; } = "i need to join conference call at tomorrow 6 pm";

        private CalendarLuis GetBaseConnectToMeetingIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.ConnectToMeeting,
            string[] subject = null,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
            string[] orderReference = null)
        {
            return GetCalendarIntent(
                userInput,
                intents,
                subject: subject,
                fromDate: fromDate,
                toDate: toDate,
                fromTime: fromTime,
                toTime: toTime,
                orderReference: orderReference);
        }
    }
}

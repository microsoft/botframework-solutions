using System;
using System.Collections.Generic;
using System.Text;
using Luis;

namespace CalendarSkillTest.Flow.Utterances
{
    public class ConnectToMeetingUtterances : BaseTestUtterances
    {
        public ConnectToMeetingUtterances()
        {
            this.Add(BaseConnectToMeeting, GetBaseConnectToMeetingIntent(BaseConnectToMeeting));
        }

        public static string BaseConnectToMeeting { get; } = "i need to join conference call";

        private CalendarLU GetBaseConnectToMeetingIntent(
            string userInput,
            CalendarLU.Intent intents = CalendarLU.Intent.ConnectToMeeting,
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

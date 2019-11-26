// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace CalendarSkill.Test.Flow.Utterances
{
    public class ChangeMeetingStatusTestUtterances : BaseTestUtterances
    {
        public ChangeMeetingStatusTestUtterances()
        {
            this.Add(BaseDeleteMeeting, GetBaseDeleteMeetingIntent(BaseDeleteMeeting));
            this.Add(DeleteMeetingWithStartTime, GetBaseDeleteMeetingIntent(
                DeleteMeetingWithStartTime,
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
            this.Add(DeleteMeetingWithTitle, GetBaseDeleteMeetingIntent(
                DeleteMeetingWithTitle,
                subject: new string[] { Strings.Strings.DefaultEventName }));
            this.Add(AcceptMeetingWithStartTime, GetBaseAcceptMeetingIntent(
                AcceptMeetingWithStartTime,
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
        }

        public static string BaseDeleteMeeting { get; } = "delete meeting";

        public static string DeleteMeetingWithStartTime { get; } = "delete meeting at tomorrow 6 pm";

        public static string DeleteMeetingWithTitle { get; } = $"delete {Strings.Strings.DefaultEventName} meeting";

        public static string AcceptMeetingWithStartTime { get; } = "accept meeting at tomorrow 6 pm";

        private CalendarLuis GetBaseDeleteMeetingIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.DeleteCalendarEntry,
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

        private CalendarLuis GetBaseAcceptMeetingIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.AcceptEventEntry,
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

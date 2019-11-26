// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Luis;

namespace CalendarSkill.Test.Flow.Utterances
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
            this.Add(FindMeetingByTitle, GetBaseFindMeetingIntent(
                FindMeetingByTitle,
                subject: new string[] { Strings.Strings.DefaultEventName }));
            this.Add(FindMeetingByAttendee, GetBaseFindMeetingIntent(
                FindMeetingByAttendee,
                contactName: new string[] { Strings.Strings.DefaultUserName }));
            this.Add(FindMeetingByLocation, GetBaseFindMeetingIntent(
                FindMeetingByLocation,
                location: new string[] { Strings.Strings.DefaultLocation }));
            this.Add(UpdateMeetingTestUtterances.BaseUpdateMeeting, UpdateMeetingTestUtterances.GetBaseUpdateMeetingIntent(UpdateMeetingTestUtterances.BaseUpdateMeeting));
        }

        public static string BaseFindMeeting { get; } = "What should I do today";

        public static string FindMeetingByTimeRange { get; } = "What's on my schedule next week";

        public static string FindMeetingByStartTime { get; } = "What are my meetings at tomorrow 6 pm";

        public static string BaseNextMeeting { get; } = "what is my next meeting";

        public static string HowLongNextMeetingMeeting { get; } = "How long is my next meeting";

        public static string WhereNextMeetingMeeting { get; } = "Where is my next meeting";

        public static string WhenNextMeetingMeeting { get; } = "When is my next meeting";

        public static string FindMeetingByTitle { get; } = $"show my the meeting about {Strings.Strings.DefaultEventName}";

        public static string FindMeetingByAttendee { get; } = $"show my the meeting with {Strings.Strings.DefaultUserName}";

        public static string FindMeetingByLocation { get; } = $"show my the meeting at {Strings.Strings.DefaultLocation}";

        private CalendarLuis GetBaseFindMeetingIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.FindCalendarEntry,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
            double[] ordinal = null,
            double[] number = null,
            string[] orderReference = null,
            string[] askParameter = null,
            string[] subject = null,
            string[] contactName = null,
            string[] location = null)
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
                askParameter: askParameter,
                subject: subject,
                contactName: contactName,
                location: location);
        }
    }
}

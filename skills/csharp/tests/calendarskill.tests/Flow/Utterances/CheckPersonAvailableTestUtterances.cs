// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace CalendarSkill.Test.Flow.Utterances
{
    public class CheckPersonAvailableTestUtterances : BaseTestUtterances
    {
        public CheckPersonAvailableTestUtterances()
        {
            this.Add(BaseCheckAvailable, GetBaseCheckAvailableIntent(
                BaseCheckAvailable,
                contactName: new string[] { Strings.Strings.DefaultUserName },
                fromDate: new string[] { "today" },
                fromTime: new string[] { "4 PM" }));
            this.Add(CheckAvailableSlotFilling, GetBaseCheckAvailableIntent(
                CheckAvailableSlotFilling,
                fromDate: new string[] { "today" }));
        }

        public static string BaseCheckAvailable { get; } = $"Is {Strings.Strings.DefaultUserName} available today at 4 PM";

        public static string CheckAvailableSlotFilling { get; } = "Check available for today";

        private CalendarLuis GetBaseCheckAvailableIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.CheckAvailability,
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

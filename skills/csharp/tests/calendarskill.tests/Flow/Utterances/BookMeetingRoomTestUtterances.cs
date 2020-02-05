// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace CalendarSkill.Test.Flow.Utterances
{
    public class BookMeetingRoomTestUtterances : BaseTestUtterances
    {
        public BookMeetingRoomTestUtterances()
        {
            this.Add(BaseBookMeetingRoom, GetBookMeetingRoomIntent(BaseBookMeetingRoom));
            this.Add(BookMeetingRoomWithMeetingRoomEntity, GetCheckAvailabilityIntent(
                BookMeetingRoomWithMeetingRoomEntity,
                meetingRoom: new string[] { Strings.Strings.DefaultMeetingRoomName }));
            this.Add(BookMeetingRoomWithBuildingEntity, GetBookMeetingRoomIntent(
                BookMeetingRoomWithBuildingEntity,
                building: new string[] { Strings.Strings.DefaultBuilding }));
            this.Add(BookMeetingRoomWithBuildingAndFloorNumberEntity, GetBookMeetingRoomIntent(
                BookMeetingRoomWithBuildingAndFloorNumberEntity,
                building: new string[] { Strings.Strings.DefaultBuilding },
                floorNumber: new string[] { Strings.Strings.DefaultFloorNumber }));
            this.Add(BookMeetingRoomWithDateTimeEntity, GetBookMeetingRoomIntent(
                BookMeetingRoomWithDateTimeEntity,
                fromDate: new string[] { Strings.Strings.DefaultStartDate },
                fromTime: new string[] { Strings.Strings.DefaultStartTime },
                toDate: new string[] { Strings.Strings.DefaultStartDate },
                toTime: new string[] { Strings.Strings.DefaultEndTime }));
            this.Add(BookMeetingRoomWithStartDateTimeEntity, GetBookMeetingRoomIntent(
                BookMeetingRoomWithStartDateTimeEntity,
                fromDate: new string[] { Strings.Strings.DefaultStartDate },
                fromTime: new string[] { Strings.Strings.DefaultStartTime }));
            this.Add(BookMeetingRoomWithDurationEntity, GetBookMeetingRoomIntent(
                BookMeetingRoomWithDurationEntity,
                duration: new string[] { Strings.Strings.DefaultDuration }));
            this.Add(ChangeMeetingRoom, GetBookMeetingRoomIntent(
                ChangeMeetingRoom));
            this.Add(ChangeMeetingRoomWithFloorNumberEntity, GetBookMeetingRoomIntent(
                ChangeMeetingRoomWithFloorNumberEntity,
                floorNumber: new string[] { Strings.Strings.DefaultFloorNumber }));
            this.Add(CancelRequest, GetCancelCalendarIntent(
                CancelRequest));
            this.Add(ChangeTime, GetChangeCalendarEntryIntent(
                ChangeTime,
                slotAttribute: new string[] { Strings.Strings.SlotAttributeTime },
                slotAttributeName: new string[][] { new string[] { Strings.Strings.SlotAttributeTime } }));
            this.Add(ChangeTimeWithDate, GetChangeCalendarEntryIntent(
                ChangeTimeWithDate,
                toDate: new string[] { Strings.Strings.DefaultStartDate }));
            this.Add(ChangeTimeWithTime, GetChangeCalendarEntryIntent(
                ChangeTimeWithTime,
                toTime: new string[] { "9 PM" }));
            this.Add(ChangeTimeWithDateAndTime, GetChangeCalendarEntryIntent(
                ChangeTimeWithDateAndTime,
                toDate: new string[] { Strings.Strings.DefaultStartDate },
                toTime: new string[] { Strings.Strings.DefaultStartTime }));
        }

        public static string BaseBookMeetingRoom { get; } = "book a meeting room";

        public static string BookMeetingRoomWithBuildingEntity { get; } = $"book a meeting room in {Strings.Strings.DefaultBuilding}";

        public static string BookMeetingRoomWithBuildingAndFloorNumberEntity { get; } = $"book a meeting room at {Strings.Strings.DefaultBuilding} on {Strings.Strings.DefaultFloorNumber}";

        public static string BookMeetingRoomWithDateTimeEntity { get; } = $"book a meeting room {Strings.Strings.DefaultStartDate} {Strings.Strings.DefaultStartTime} to {Strings.Strings.DefaultStartDate} {Strings.Strings.DefaultEndTime}";

        public static string BookMeetingRoomWithStartDateTimeEntity { get; } = $"book a meeting room {Strings.Strings.DefaultStartDate} {Strings.Strings.DefaultStartTime}";

        public static string BookMeetingRoomWithDurationEntity { get; } = $"book a meeting room for {Strings.Strings.DefaultDuration}";

        public static string ChangeMeetingRoom { get; } = "change a different room";

        public static string ChangeMeetingRoomWithFloorNumberEntity { get; } = $"book a meeting room on {Strings.Strings.DefaultFloorNumber}";

        public static string CancelRequest { get; } = "cancel request";

        public static string BookMeetingRoomWithMeetingRoomEntity { get; } = $"is the room {Strings.Strings.DefaultMeetingRoomName} open";

        public static string ChangeTime { get; } = $"change a different {Strings.Strings.SlotAttributeTime}";

        public static string ChangeTimeWithDate { get; } = $"change time to {Strings.Strings.DefaultStartDate}";

        public static string ChangeTimeWithTime { get; } = $"change time to 9 PM";

        public static string ChangeTimeWithDateAndTime { get; } = $"change time to {Strings.Strings.DefaultStartTime} {Strings.Strings.DefaultStartDate}";

        private CalendarLuis GetBookMeetingRoomIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.FindMeetingRoom,
            string[] subject = null,
            string[] contactName = null,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
            string[] duration = null,
            string[] meetingRoom = null,
            string[] building = null,
            string[] floorNumber = null,
            string[] location = null,
            double[] ordinal = null,
            double[] number = null)
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
                building: building,
                floorNumber: floorNumber,
                location: location,
                ordinal: ordinal,
                number: number);
        }

        private CalendarLuis GetCheckAvailabilityIntent(
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
            string[] building = null,
            string[] floorNumber = null,
            string[] location = null,
            double[] ordinal = null,
            double[] number = null)
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
                building: building,
                floorNumber: floorNumber,
                location: location,
                ordinal: ordinal,
                number: number);
        }

        private CalendarLuis GetCancelCalendarIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.CancelCalendar,
            string[] subject = null,
            string[] contactName = null,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
            string[] duration = null,
            string[] meetingRoom = null,
            string[] building = null,
            string[] floorNumber = null,
            string[] location = null,
            double[] ordinal = null,
            double[] number = null)
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
                building: building,
                floorNumber: floorNumber,
                location: location,
                ordinal: ordinal,
                number: number);
        }

        private CalendarLuis GetChangeCalendarEntryIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.ChangeCalendarEntry,
            string[] subject = null,
            string[] contactName = null,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
            string[] duration = null,
            string[] meetingRoom = null,
            string[] building = null,
            string[] floorNumber = null,
            string[] location = null,
            string[] slotAttribute = null,
            string[][] slotAttributeName = null,
            double[] ordinal = null,
            double[] number = null)
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
                building: building,
                floorNumber: floorNumber,
                location: location,
                ordinal: ordinal,
                number: number,
                slotAttribute: slotAttribute,
                slotAttributeName: slotAttributeName);
        }
    }
}

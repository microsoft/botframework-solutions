using Luis;

namespace CalendarSkill.Test.Flow.Utterances
{
    public class UpdateMeetingRoomTestUtterances : BaseTestUtterances
    {
        public UpdateMeetingRoomTestUtterances()
        {
            this.Add(ChangeMeetingRoomWithStartTime, GetBaseUpdateMeetingIntent(
                ChangeMeetingRoomWithStartTime,
                intents: CalendarLuis.Intent.ChangeCalendarEntry,
                slotAttribute: new string[] { Strings.Strings.SlotAttributeRoom },
                slotAttributeName: new string[][] { new string[] { Strings.Strings.SlotAttributeRoom } },
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
            this.Add(AddMeetingRoomWithStartTime, GetBaseUpdateMeetingIntent(
                AddMeetingRoomWithStartTime,
                intents: CalendarLuis.Intent.AddCalendarEntryAttribute,
                slotAttribute: new string[] { Strings.Strings.SlotAttributeRoom },
                slotAttributeName: new string[][] { new string[] { Strings.Strings.SlotAttributeRoom } },
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
            this.Add(CancelMeetingRoomWithStartTime, GetBaseUpdateMeetingIntent(
                CancelMeetingRoomWithStartTime,
                intents: CalendarLuis.Intent.DeleteCalendarEntry,
                slotAttribute: new string[] { Strings.Strings.SlotAttributeRoom },
                slotAttributeName: new string[][] { new string[] { Strings.Strings.SlotAttributeRoom } },
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
        }

        public static string BaseUpdateMeeting { get; } = "update meeting";

        public static string ChangeMeetingRoomWithStartTime { get; } = "change meeting room of meeting at tomorrow 6 pm";

        public static string AddMeetingRoomWithStartTime { get; } = "add meeting room of meeting at tomorrow 6 pm";

        public static string CancelMeetingRoomWithStartTime { get; } = "cancel meeting room of meeting at tomorrow 6 pm";

        public static CalendarLuis GetBaseUpdateMeetingIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.ChangeCalendarEntry,
            string[] subject = null,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
            string[] slotAttribute = null,
            string[][] slotAttributeName = null,
            string[] moveEarlierTimeSpan = null,
            string[] moveLaterTimeSpan = null)
        {
            return GetCalendarIntent(
                userInput,
                intents,
                subject: subject,
                fromDate: fromDate,
                toDate: toDate,
                fromTime: fromTime,
                toTime: toTime,
                slotAttribute: slotAttribute,
                slotAttributeName: slotAttributeName,
                moveEarlierTimeSpan: moveEarlierTimeSpan,
                moveLaterTimeSpan: moveLaterTimeSpan);
        }
    }
}

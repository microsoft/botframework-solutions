using Luis;

namespace CalendarSkill.Test.Flow.Utterances
{
    public class UpdateMeetingRoomTestUtterances : BaseTestUtterances
    {
        public UpdateMeetingRoomTestUtterances()
        {
            this.Add(ChangeMeetingRoomWithStartTime, GetBaseUpdateMeetingIntent(
                ChangeMeetingRoomWithStartTime,
                intents: CalendarLuis.Intent.ChangeMeetingRoom,
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
            this.Add(AddMeetingRoomWithStartTime, GetBaseUpdateMeetingIntent(
                AddMeetingRoomWithStartTime,
                intents: CalendarLuis.Intent.AddMeetingRoom,
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
            this.Add(CancelMeetingRoomWithStartTime, GetBaseUpdateMeetingIntent(
                CancelMeetingRoomWithStartTime,
                intents: CalendarLuis.Intent.CancelMeetingRoom,
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
        }

        public static string BaseUpdateMeeting { get; } = "update meeting";

        public static string ChangeMeetingRoomWithStartTime { get; } = "change meeting room of meeting at tomorrow 6 pm";

        public static string AddMeetingRoomWithStartTime { get; } = "add meeting room of meeting at tomorrow 6 pm";

        public static string CancelMeetingRoomWithStartTime { get; } = "cancel meeting room of meeting at tomorrow 6 pm";

        public static CalendarLuis GetBaseUpdateMeetingIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.ChangeMeetingRoom,
            string[] subject = null,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
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
                moveEarlierTimeSpan: moveEarlierTimeSpan,
                moveLaterTimeSpan: moveLaterTimeSpan);
        }
    }
}

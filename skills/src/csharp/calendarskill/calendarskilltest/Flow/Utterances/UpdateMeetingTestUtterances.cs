using Luis;

namespace CalendarSkillTest.Flow.Utterances
{
    public class UpdateMeetingTestUtterances : BaseTestUtterances
    {
        public UpdateMeetingTestUtterances()
        {
            this.Add(BaseUpdateMeeting, GetBaseUpdateMeetingIntent(BaseUpdateMeeting));
            this.Add(UpdateMeetingWithStartTime, GetBaseUpdateMeetingIntent(
                UpdateMeetingWithStartTime,
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
            this.Add(UpdateMeetingWithTitle, GetBaseUpdateMeetingIntent(
                UpdateMeetingWithTitle,
                subject: new string[] { Strings.Strings.DefaultEventName }));
            this.Add(UpdateMeetingWithMoveEarlierTimeSpan, GetBaseUpdateMeetingIntent(
                UpdateMeetingWithMoveEarlierTimeSpan,
                subject: new string[] { Strings.Strings.DefaultEventName },
                moveEarlierTimeSpan: new string[] { "30 minutes" }));
            this.Add(UpdateMeetingWithMoveLaterTimeSpan, GetBaseUpdateMeetingIntent(
                UpdateMeetingWithMoveLaterTimeSpan,
                subject: new string[] { Strings.Strings.DefaultEventName },
                moveLaterTimeSpan: new string[] { "30 minutes" }));
        }

        public static string BaseUpdateMeeting { get; } = "update meeting";

        public static string UpdateMeetingWithStartTime { get; } = "update meeting at tomorrow 6 pm";

        public static string UpdateMeetingWithTitle { get; } = $"update {Strings.Strings.DefaultEventName} meeting";

        public static string UpdateMeetingWithMoveEarlierTimeSpan { get; } = $"update {Strings.Strings.DefaultEventName} meeting 30 minutes earlier";

        public static string UpdateMeetingWithMoveLaterTimeSpan { get; } = $"update {Strings.Strings.DefaultEventName} meeting 30 minutes later";

        public static CalendarLuis GetBaseUpdateMeetingIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.ChangeCalendarEntry,
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

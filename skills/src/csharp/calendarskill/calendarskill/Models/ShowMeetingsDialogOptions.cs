namespace CalendarSkill.Models
{
    public class ShowMeetingsDialogOptions : CalendarSkillDialogOptions
    {
        public ShowMeetingsDialogOptions()
        {
            Reason = ShowMeetingReason.FirstShowOverview;
        }

        public ShowMeetingsDialogOptions(ShowMeetingReason reason, object options)
        {
            var calendarOptions = options as CalendarSkillDialogOptions;
            Reason = reason;
            SkillMode = calendarOptions == null ? false : calendarOptions.SkillMode;
        }

        public enum ShowMeetingReason
        {
            /// <summary>
            /// FirstShowOverview.
            /// </summary>
            FirstShowOverview,

            /// <summary>
            /// ShowOverviewAgain.
            /// </summary>
            ShowOverviewAgain,

            /// <summary>
            /// ShowFilteredByTitleMeetings.
            /// </summary>
            ShowFilteredByTitleMeetings,

            /// <summary>
            /// ShowFilteredByTimeMeetings.
            /// </summary>
            ShowFilteredByTimeMeetings,

            /// <summary>
            /// ShowFilteredByParticipantNameMeetings.
            /// </summary>
            ShowFilteredByParticipantNameMeetings
        }

        public ShowMeetingReason Reason { get; set; }
    }
}

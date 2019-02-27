﻿namespace CalendarSkill.Dialogs.Shared.DialogOptions
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
            /// ShowFilteredMeetings.
            /// </summary>
            ShowFilteredMeetings
        }

        public ShowMeetingReason Reason { get; set; }
    }
}

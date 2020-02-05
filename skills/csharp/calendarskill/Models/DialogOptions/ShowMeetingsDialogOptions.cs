// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkill.Models.DialogOptions
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
        }

        public enum ShowMeetingReason
        {
            /// <summary>
            /// FirstShowOverview.
            /// </summary>
            FirstShowOverview,

            /// <summary>
            /// ShowOverviewAfterPageTurning.
            /// </summary>
            ShowOverviewAfterPageTurning,

            /// <summary>
            /// ShowOverviewAgain.
            /// </summary>
            ShowOverviewAgain,

            /// <summary>
            /// ShowFilteredMeetings.
            /// </summary>
            ShowFilteredMeetings,

            /// <summary>
            /// ShowNextMeeting.
            /// </summary>
            ShowNextMeeting
        }

        public ShowMeetingReason Reason { get; set; }
    }
}
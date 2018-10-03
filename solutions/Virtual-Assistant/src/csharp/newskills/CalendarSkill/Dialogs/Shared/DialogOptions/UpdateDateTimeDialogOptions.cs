// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CalendarSkill
{
    public class UpdateDateTimeDialogOptions
    {
        public UpdateDateTimeDialogOptions()
        {
            Reason = UpdateReason.NotFound;
        }

        public UpdateDateTimeDialogOptions(UpdateReason reason)
        {
            Reason = reason;
        }

        public enum UpdateReason
        {
            /// <summary>
            /// NotADateTime.
            /// </summary>
            NotADateTime,

            /// <summary>
            /// NotFound.
            /// </summary>
            NotFound,

            /// <summary>
            /// NoEvent.
            /// </summary>
            NoEvent,
        }

        public bool SkillMode { get; set; }

        public UpdateReason Reason { get; set; }
    }
}

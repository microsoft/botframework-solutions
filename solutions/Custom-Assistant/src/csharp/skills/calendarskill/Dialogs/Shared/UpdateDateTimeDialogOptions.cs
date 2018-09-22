// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CalendarSkill
{
    public class UpdateDateTimeDialogOptions
    {
        public UpdateDateTimeDialogOptions()
        {
            this.Reason = UpdateReason.NotFound;
        }

        public UpdateDateTimeDialogOptions(UpdateReason reason)
        {
            this.Reason = reason;
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

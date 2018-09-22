// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CalendarSkill
{
    public class UpdateUserNameDialogOptions
    {
        public UpdateUserNameDialogOptions()
        {
            this.Reason = UpdateReason.NotFound;
        }

        public UpdateUserNameDialogOptions(UpdateReason reason)
        {
            this.Reason = reason;
        }

        public enum UpdateReason
        {
            /// <summary>
            /// NotADateTime.
            /// </summary>
            TooMany,

            /// <summary>
            /// NotFound.
            /// </summary>
            NotFound,
        }

        public bool SkillMode { get; set; }

        public UpdateReason Reason { get; set; }
    }
}

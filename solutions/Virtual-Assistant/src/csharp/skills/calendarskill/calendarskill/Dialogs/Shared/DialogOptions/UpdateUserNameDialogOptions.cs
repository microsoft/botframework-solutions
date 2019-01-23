// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CalendarSkill.Dialogs.Shared.DialogOptions
{
    public class UpdateUserNameDialogOptions
    {
        public UpdateUserNameDialogOptions()
        {
            Reason = UpdateReason.TooMany;
        }

        public UpdateUserNameDialogOptions(UpdateReason reason)
        {
            Reason = reason;
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

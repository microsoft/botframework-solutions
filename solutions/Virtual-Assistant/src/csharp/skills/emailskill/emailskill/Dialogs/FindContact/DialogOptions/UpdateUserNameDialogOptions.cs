// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill.Dialogs.FindContact.DialogOptions
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
            /// Too many people with the same name.
            /// </summary>
            TooMany,

            /// <summary>
            /// The person not found.
            /// </summary>
            NotFound,

            /// <summary>
            /// Confirm no.
            /// </summary>
            ConfirmNo,

            /// <summary>
            /// ConfirmNo.
            /// </summary>
            Initialize,
        }

        public bool SkillMode { get; set; }

        public UpdateReason Reason { get; set; }
    }
}
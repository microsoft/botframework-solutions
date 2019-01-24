// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill.Dialogs.Shared.DialogOptions
{
    public class UpdateUserDialogOptions
    {
        public UpdateUserDialogOptions()
        {
            this.Reason = UpdateReason.TooMany;
        }

        public UpdateUserDialogOptions(UpdateReason reason)
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
        }

        public UpdateReason Reason { get; set; }
    }
}
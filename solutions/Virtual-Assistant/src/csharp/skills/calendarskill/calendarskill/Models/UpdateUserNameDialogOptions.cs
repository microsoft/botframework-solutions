﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CalendarSkill.Models
{
    public class UpdateUserNameDialogOptions
    {
        public UpdateUserNameDialogOptions()
        {
            Reason = UpdateReason.NotFound;
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

            /// <summary>
            /// ConfirmNo.
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using EmailSkill.Dialogs.Shared.DialogOptions;

namespace EmailSkill.Dialogs.FindContact.DialogOptions
{
    public class FindContactDialogOptions : EmailSkillDialogOptions
    {
        public FindContactDialogOptions()
        {
            FindContactReason = FindContactReasonType.FirstFindContact;
        }

        public FindContactDialogOptions(
            object options,
            FindContactReasonType findContactReason = FindContactReasonType.FirstFindContact,
            UpdateUserNameReasonType updateUserNameReason = UpdateUserNameReasonType.NotFound,
            bool promptMoreContact = true)
        {
            var calendarOptions = options as EmailSkillDialogOptions;
            FindContactReason = findContactReason;
            UpdateUserNameReason = updateUserNameReason;
            PromptMoreContact = promptMoreContact;
            SkillMode = calendarOptions == null ? false : calendarOptions.SkillMode;
        }

        public enum FindContactReasonType
        {
            /// <summary>
            /// FirstFindContact.
            /// </summary>
            FirstFindContact,

            /// <summary>
            /// FindContactAgain.
            /// </summary>
            FindContactAgain,
        }

        public enum UpdateUserNameReasonType
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

        public FindContactReasonType FindContactReason { get; set; }

        public UpdateUserNameReasonType UpdateUserNameReason { get; set; }

        public bool PromptMoreContact { get; set; }
    }
}
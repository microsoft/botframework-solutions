// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using EmailSkill.Models.DialogModel;

namespace EmailSkill.Models
{
    public class FindContactDialogOptions : EmailSkillDialogOptions
    {
        public FindContactDialogOptions()
        {
            FindContactReason = FindContactReasonType.FirstFindContact;
            DialogState = null;
            SubFlowMode = true;
        }

        public FindContactDialogOptions(
            object options,
            FindContactReasonType findContactReason = FindContactReasonType.FirstFindContact,
            UpdateUserNameReasonType updateUserNameReason = UpdateUserNameReasonType.NotFound,
            bool promptMoreContact = true)
        {
            var emailOptions = options as EmailSkillDialogOptions;
            FindContactReason = findContactReason;
            UpdateUserNameReason = updateUserNameReason;
            PromptMoreContact = promptMoreContact;

            DialogState = emailOptions.DialogState;
            SubFlowMode = emailOptions.SubFlowMode;
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

        public EmailStateBase DialogState { get; set; } = null;

        public bool SubFlowMode { get; set; } = false;
    }
}
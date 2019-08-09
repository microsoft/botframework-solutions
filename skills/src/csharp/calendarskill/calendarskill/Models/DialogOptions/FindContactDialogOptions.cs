namespace CalendarSkill.Models.DialogOptions
{
    public class FindContactDialogOptions : CalendarSkillDialogOptions
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
            var calendarOptions = options as CalendarSkillDialogOptions;
            FindContactReason = findContactReason;
            UpdateUserNameReason = updateUserNameReason;
            PromptMoreContact = promptMoreContact;
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

        public FindContactReasonType FindContactReason { get; set; }

        public UpdateUserNameReasonType UpdateUserNameReason { get; set; }

        public bool PromptMoreContact { get; set; }
    }
}

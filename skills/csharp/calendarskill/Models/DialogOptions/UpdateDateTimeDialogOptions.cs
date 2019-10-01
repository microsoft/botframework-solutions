namespace CalendarSkill.Models.DialogOptions
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

        public UpdateReason Reason { get; set; }
    }
}
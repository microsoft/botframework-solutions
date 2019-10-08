namespace Microsoft.Bot.Builder.Solutions.Dialogs
{
    /// <summary>
    /// Define router dialog turn result.
    /// </summary>
    public class RouterDialogTurnResult
    {
        public RouterDialogTurnResult(RouterDialogTurnStatus status)
        {
            this.Status = status;
        }

        public RouterDialogTurnStatus Status { get; set; }
    }
}

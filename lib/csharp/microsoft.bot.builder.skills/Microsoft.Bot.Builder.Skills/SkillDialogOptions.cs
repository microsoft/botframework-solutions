namespace Microsoft.Bot.Builder.Skills
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;

    public class SkillDialogOptions
    {
        public string ActionName { get; set; }

        public Func<DialogContext, Task<bool>> FallbackEventCallback { get; set; }

        public bool DoConfirmation { get; set; } = false;
    }
}

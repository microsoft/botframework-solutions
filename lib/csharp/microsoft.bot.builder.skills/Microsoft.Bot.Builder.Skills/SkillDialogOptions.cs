using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillDialogOptions
    {
        public string ActionName { get; set; }

        public Func<DialogContext, Task<bool>> FallbackEventCallback { get; set; }

        public bool DoConfirmation { get; set; } = false;
    }
}

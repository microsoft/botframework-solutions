using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Skills
{
    public interface ISkillIntentRecognizer
    {
        Func<DialogContext, Task<string>> RecognizeSkillIntentAsync { get; }

        bool ConfirmIntentSwitch { get; }
    }
}

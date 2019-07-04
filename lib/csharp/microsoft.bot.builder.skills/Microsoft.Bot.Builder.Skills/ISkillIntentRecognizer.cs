using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills
{
    public interface ISkillIntentRecognizer
    {
        Func<DialogContext, Task<SkillManifest>> RecognizeSkillIntentAsync { get; }

        bool ConfirmIntentSwitch { get; }
    }
}

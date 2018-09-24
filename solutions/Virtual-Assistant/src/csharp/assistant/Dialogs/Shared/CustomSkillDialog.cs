using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualAssistant
{
    public class CustomSkillDialog : ComponentDialog
    {
        public CustomSkillDialog(BotServices botServices)
            : base(nameof(CustomSkillDialog))
        {
            AddDialog(new SkillDialog(botServices.CosmosDbOptions, botServices.TelemetryClient));
        }

        protected override Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            return outerDc.EndDialogAsync();
        }
    }
}

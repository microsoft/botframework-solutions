using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Solutions.Skills;

namespace VirtualAssistant
{
    public class CustomSkillDialog : ComponentDialog
    {
        public CustomSkillDialog(Dictionary<string, ISkillConfiguration> skills, IStatePropertyAccessor<DialogState> accessor, EndpointService endpointService, IBotTelemetryClient telemetryClient)
            : base(nameof(CustomSkillDialog))
        {
            AddDialog(new SkillDialog(skills, accessor, endpointService, telemetryClient));
        }

        protected override Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            return outerDc.EndDialogAsync();
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Models.Manifest;

namespace VirtualAssistantSample.Dialogs
{
    public class IntentSwitchDialog : ComponentDialog
    {
        public IntentSwitchDialog()
            : base(nameof(IntentSwitchDialog))
        {
            var intentSwitch = new WaterfallStep[]
            {
                PromptToSwitch,
                End,
            };

            AddDialog(new WaterfallDialog(nameof(IntentSwitchDialog), intentSwitch));
            AddDialog(new ConfirmPrompt("ConfirmIntentSwitch"));
        }

        private async Task<DialogTurnResult> PromptToSwitch(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            dynamic options = stepContext.Options;
            SkillManifest newSkill = options.newSkill;

            return await stepContext.PromptAsync("ConfirmIntentSwitch", new PromptOptions()
            {
                Prompt = MessageFactory.Text($"I found the following skill that can handle your request. Would you like to switch? \n\n * {newSkill.Name}")
            });
        }

        private async Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            bool result = (bool)stepContext.Result;
            return await stepContext.EndDialogAsync(result: result);
        }

        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            return new DialogTurnResult(DialogTurnStatus.Complete, result);
        }
    }
}

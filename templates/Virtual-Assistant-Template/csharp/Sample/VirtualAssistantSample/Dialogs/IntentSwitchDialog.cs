using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace VirtualAssistantSample.Dialogs
{
    public class IntentSwitchDialog : ComponentDialog
    {
        private LocaleTemplateEngineManager _templateEngine;

        public IntentSwitchDialog(IServiceProvider serviceProvider)
            : base(nameof(IntentSwitchDialog))
        {
            _templateEngine = serviceProvider.GetService<LocaleTemplateEngineManager>();

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
                Prompt = _templateEngine.GenerateActivityForLocale("IntentSwitchPrompt", new { Skill = newSkill.Name })
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

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace VirtualAssistantSample.Dialogs
{
    public class SkillSwitchDialog : ComponentDialog
    {
        private LocaleTemplateEngineManager _templateEngine;
        private IStatePropertyAccessor<string> _skillIdAccessor;
        private IStatePropertyAccessor<Activity> _lastActivityAccessor;

        public SkillSwitchDialog(
            ConversationState conversationState,
            LocaleTemplateEngineManager templateEngine)
            : base(nameof(SkillSwitchDialog))
        {
            _skillIdAccessor = conversationState.CreateProperty<string>(Properties.SkillId);
            _lastActivityAccessor = conversationState.CreateProperty<Activity>(Properties.LastActivity);
            _templateEngine = templateEngine;

            var intentSwitch = new WaterfallStep[]
            {
                PromptToSwitch,
                End,
            };

            AddDialog(new WaterfallDialog(nameof(SkillSwitchDialog), intentSwitch));
            AddDialog(new ConfirmPrompt("ConfirmIntentSwitch"));
        }

        // Runs when this dialog ends. Handles result of prompt to switch skills or resume waiting dialog.
        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            var skillId = await _skillIdAccessor.GetAsync(outerDc.Context, () => null);
            var lastActivity = await _lastActivityAccessor.GetAsync(outerDc.Context, () => null);
            outerDc.Context.Activity.Text = lastActivity.Text;

            // Ends this dialog.
            await outerDc.EndDialogAsync();

            if ((bool)result)
            {
                // If user decided to switch, replace current skill dialog with new skill dialog.
                return await outerDc.ReplaceDialogAsync(skillId);
            }
            else
            {
                // Otherwise, continue the waiting skill dialog with the user's previous utterance.
                return await outerDc.ContinueDialogAsync();
            }
        }

        // Prompts user to switch to a new skill.
        private async Task<DialogTurnResult> PromptToSwitch(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = stepContext.Options as SkillSwitchDialogOptions ?? throw new ArgumentException($"You must provide options of type {typeof(SkillSwitchDialogOptions).FullName}.");
            await _skillIdAccessor.SetAsync(stepContext.Context, options.Skill.Id);
            await _lastActivityAccessor.SetAsync(stepContext.Context, stepContext.Context.Activity);

            return await stepContext.PromptAsync("ConfirmIntentSwitch", new PromptOptions() { Prompt = options.Prompt });
        }

        // Ends this dialog, returning the prompt result.
        private async Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            bool result = (bool)stepContext.Result;
            return await stepContext.EndDialogAsync(result: result);
        }

        private class Properties
        {
            public const string SkillId = "skillSwitchValue";
            public const string LastActivity = "skillSwitchActivity";
        }
    }
}

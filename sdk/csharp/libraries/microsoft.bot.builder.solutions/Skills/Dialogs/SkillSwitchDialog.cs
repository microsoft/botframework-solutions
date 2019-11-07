using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Skills.Dialogs
{
    public class SkillSwitchDialog : ComponentDialog
    {
        private static string _confirmPromptId = "ConfirmSkillSwitch";
        private IStatePropertyAccessor<string> _skillIdAccessor;
        private IStatePropertyAccessor<Activity> _lastActivityAccessor;

        public SkillSwitchDialog(ConversationState conversationState)
            : base(nameof(SkillSwitchDialog))
        {
            _skillIdAccessor = conversationState.CreateProperty<string>(Properties.SkillId);
            _lastActivityAccessor = conversationState.CreateProperty<Activity>(Properties.LastActivity);

            var intentSwitch = new WaterfallStep[]
            {
                PromptToSwitchAsync,
                EndAsync,
            };

            AddDialog(new WaterfallDialog(nameof(SkillSwitchDialog), intentSwitch));
            AddDialog(new ConfirmPrompt(_confirmPromptId));
        }

        // Runs when this dialog ends. Handles result of prompt to switch skills or resume waiting dialog.
        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            var skillId = await _skillIdAccessor.GetAsync(outerDc.Context, () => null).ConfigureAwait(false);
            var lastActivity = await _lastActivityAccessor.GetAsync(outerDc.Context, () => null).ConfigureAwait(false);
            outerDc.Context.Activity.Text = lastActivity.Text;

            // Ends this dialog.
            await outerDc.EndDialogAsync().ConfigureAwait(false);

            if ((bool)result)
            {
                // If user decided to switch, replace current skill dialog with new skill dialog.
                return await outerDc.ReplaceDialogAsync(skillId).ConfigureAwait(false);
            }
            else
            {
                // Otherwise, continue the waiting skill dialog with the user's previous utterance.
                return await outerDc.ContinueDialogAsync().ConfigureAwait(false);
            }
        }

        // Prompts user to switch to a new skill.
        private async Task<DialogTurnResult> PromptToSwitchAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = stepContext.Options as SkillSwitchDialogOptions ?? throw new ArgumentException($"You must provide options of type {typeof(SkillSwitchDialogOptions).FullName}.");
            await _skillIdAccessor.SetAsync(stepContext.Context, options.Skill.Id).ConfigureAwait(false);
            await _lastActivityAccessor.SetAsync(stepContext.Context, stepContext.Context.Activity).ConfigureAwait(false);

            return await stepContext.PromptAsync(_confirmPromptId, new PromptOptions() { Prompt = options.Prompt }).ConfigureAwait(false);
        }

        // Ends this dialog, returning the prompt result.
        private async Task<DialogTurnResult> EndAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            bool result = (bool)stepContext.Result;
            return await stepContext.EndDialogAsync(result: result).ConfigureAwait(false);
        }

        private class Properties
        {
            public const string SkillId = "skillSwitchValue";
            public const string LastActivity = "skillSwitchActivity";
        }
    }
}
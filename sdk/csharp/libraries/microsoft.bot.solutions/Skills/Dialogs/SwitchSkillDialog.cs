// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Skills.Dialogs
{
    public class SwitchSkillDialog : ComponentDialog
    {
        private static string _confirmPromptId = "ConfirmSkillSwitch";
        private IStatePropertyAccessor<string> _skillIdAccessor;
        private IStatePropertyAccessor<Activity> _lastActivityAccessor;

        public SwitchSkillDialog(ConversationState conversationState)
            : base(nameof(SwitchSkillDialog))
        {
            _skillIdAccessor = conversationState.CreateProperty<string>(Properties.SkillId);
            _lastActivityAccessor = conversationState.CreateProperty<Activity>(Properties.LastActivity);

            var intentSwitch = new WaterfallStep[]
            {
                PromptToSwitchAsync,
                EndAsync,
            };

            AddDialog(new WaterfallDialog(nameof(SwitchSkillDialog), intentSwitch));
            AddDialog(new ConfirmPrompt(_confirmPromptId));
        }

        // Runs when this dialog ends. Handles result of prompt to switch skills or resume waiting dialog.
        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            var skillId = await _skillIdAccessor.GetAsync(outerDc.Context, () => null).ConfigureAwait(false);
            var lastActivity = await _lastActivityAccessor.GetAsync(outerDc.Context, () => null).ConfigureAwait(false);
            outerDc.Context.Activity.Text = lastActivity.Text;

            if ((bool)result)
            {
                // If user decided to switch, replace current skill dialog with new skill dialog.
                var skillDialogOptions = new BeginSkillDialogOptions { Activity = outerDc.Context.Activity };

                // End the SwitchSkillDialog without triggering the ResumeDialog function of current SkillDialog
                outerDc.Stack.RemoveAt(0);

                // Start the skill dialog.
                return await outerDc.ReplaceDialogAsync(skillId, skillDialogOptions).ConfigureAwait(false);
            }
            else
            {
                // Ends this dialog.
                return await outerDc.EndDialogAsync().ConfigureAwait(false);
            }
        }

        // Prompts user to switch to a new skill.
        private async Task<DialogTurnResult> PromptToSwitchAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = stepContext.Options as SwitchSkillDialogOptions ?? throw new ArgumentException($"You must provide options of type {typeof(SwitchSkillDialogOptions).FullName}.");
            await _skillIdAccessor.SetAsync(stepContext.Context, options.Skill.Id).ConfigureAwait(false);
            await _lastActivityAccessor.SetAsync(stepContext.Context, stepContext.Context.Activity).ConfigureAwait(false);

            return await stepContext.PromptAsync(_confirmPromptId, options).ConfigureAwait(false);
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

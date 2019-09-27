// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;

namespace VirtualAssistantSample.Dialogs
{
    public class CancelDialog : ComponentDialog
    {
        public CancelDialog()
            : base(nameof(CancelDialog))
        {
            InitialDialogId = nameof(CancelDialog);

            var cancel = new WaterfallStep[]
            {
                AskToCancel,
                FinishCancelDialog,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, cancel));
            AddDialog(new ConfirmPrompt(DialogIds.CancelPrompt));
        }

        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            var activityGenerator = outerDc.Context.TurnState.Get<IActivityGenerator>();

            var doCancel = (bool)result;

            if (doCancel)
            {
                // If user chose to cancel
                var activity = await activityGenerator.Generate(outerDc.Context, "cancelConfirmedMessage", null);
                await outerDc.Context.SendActivityAsync(activity);

                // Cancel all in outer stack of component i.e. the stack the component belongs to
                return await outerDc.CancelAllDialogsAsync();
            }
            else
            {
                // else if user chose not to cancel
                var activity = await activityGenerator.Generate(outerDc.Context, "cancelDeniedMessage", null);
                await outerDc.Context.SendActivityAsync(activity);

                // End this component. Will trigger reprompt/resume on outer stack
                return await outerDc.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> AskToCancel(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var activityGenerator = sc.Context.TurnState.Get<IActivityGenerator>();
            var activity = await activityGenerator.Generate(sc.Context, "cancelPrompt", null);

            return await sc.PromptAsync(DialogIds.CancelPrompt, new PromptOptions()
            {
                Prompt = activity
            });
        }

        private async Task<DialogTurnResult> FinishCancelDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.EndDialogAsync((bool)sc.Result);
        }

        private class DialogIds
        {
            public const string CancelPrompt = "cancelPrompt";
        }
    }
}

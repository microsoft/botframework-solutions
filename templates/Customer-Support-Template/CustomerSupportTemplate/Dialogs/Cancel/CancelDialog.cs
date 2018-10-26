// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using CustomerSupportTemplate.Dialogs.Shared;
using Microsoft.Bot.Builder.Dialogs;

namespace CustomerSupportTemplate
{
    public class CancelDialog : ComponentDialog
    {
        private static CancelResponses _responder = new CancelResponses();

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
            AddDialog(new ConfirmPrompt(DialogIds.CancelPrompt, SharedValidators.ConfirmValidator));
        }

        public async Task<DialogTurnResult> AskToCancel(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.CancelPrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, CancelResponses.ResponseIds.ConfirmCancelPrompt),
            });
        }

        public async Task<DialogTurnResult> FinishCancelDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync((bool)stepContext.Result);
        }

        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            var doCancel = (bool)result;

            if (doCancel)
            {
                // If user chose to cancel
                await _responder.ReplyWith(outerDc.Context, CancelResponses.ResponseIds.CancelConfirmedMessage);

                // Cancel all in outer stack of component i.e. the stack the component belongs to
                return await outerDc.CancelAllDialogsAsync();
            }
            else
            {
                // else if user chose not to cancel
                await _responder.ReplyWith(outerDc.Context, CancelResponses.ResponseIds.CancelDeniedMessage);

                // End this component. Will trigger reprompt/resume on outer stack
                return await outerDc.EndDialogAsync();
            }
        }

        private class DialogIds
        {
            public const string CancelPrompt = "cancelPrompt";
        }
    }
}

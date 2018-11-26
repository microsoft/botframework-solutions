// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace ToDoSkill
{
    public class CancelDialog : ComponentDialog
    {
        // Constants
        public const string CancelPrompt = "cancelPrompt";

        // Fields
        private static CancelResponses _responder = new CancelResponses();

        public CancelDialog()
            : base(nameof(CancelDialog))
        {
            InitialDialogId = nameof(CancelDialog);

            var cancel = new WaterfallStep[]
            {
                FinishCancelDialog,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, cancel));
        }

        public static async Task<DialogTurnResult> FinishCancelDialog(WaterfallStepContext sc, CancellationToken cancellationToken) => await sc.EndDialogAsync(true);

        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            // If user chose to cancel
            await _responder.ReplyWith(outerDc.Context, CancelResponses._cancelConfirmed);

            // Cancel all in outer stack of component i.e. the stack the component belongs to
            return await outerDc.CancelAllDialogsAsync();
        }
    }
}

﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace CalendarSkill
{
    public class CancelDialog : ComponentDialog
    {
        // Constants
        public const string CancelPrompt = "cancelPrompt";

        // Fields
        private CancelResponses _responder = new CancelResponses();
        private IStatePropertyAccessor<CalendarSkillState> _accessor;

        public CancelDialog(IStatePropertyAccessor<CalendarSkillState> accessor)
            : base(nameof(CancelDialog))
        {
            InitialDialogId = nameof(CancelDialog);
            _accessor = accessor;

            var cancel = new WaterfallStep[]
            {
                AskToCancel,
                FinishCancelDialog,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, cancel));
            AddDialog(new ConfirmPrompt(CancelPrompt));
        }

        public async Task<DialogTurnResult> AskToCancel(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.PromptAsync(CancelPrompt, new PromptOptions()
            {
                Prompt = await _responder.RenderTemplate(sc.Context, "en", CancelResponses._confirmPrompt),
            });
        }

        public async Task<DialogTurnResult> FinishCancelDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await _accessor.GetAsync(sc.Context);
            state.Clear();
            return await sc.EndDialogAsync((bool)sc.Result);
        }

        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            var doCancel = (bool)result;

            if (doCancel)
            {
                // If user chose to cancel
                await _responder.ReplyWith(outerDc.Context, CancelResponses._cancelConfirmed);

                // Cancel all in outer stack of component i.e. the stack the component belongs to
                return await outerDc.CancelAllDialogsAsync();
            }
            else
            {
                // else if user chose not to cancel
                await _responder.ReplyWith(outerDc.Context, CancelResponses._cancelDenied);

                // End this component. Will trigger reprompt/resume on outer stack
                return await outerDc.EndDialogAsync();
            }
        }
    }
}

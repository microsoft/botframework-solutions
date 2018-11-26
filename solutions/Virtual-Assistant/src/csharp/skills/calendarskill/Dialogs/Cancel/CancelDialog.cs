// Copyright (c) Microsoft Corporation. All rights reserved.
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
                FinishCancelDialog,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, cancel));
        }

        public async Task<DialogTurnResult> FinishCancelDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await _accessor.GetAsync(sc.Context);
            state.Clear();
            return await sc.EndDialogAsync(true);
        }

        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            // If user chose to cancel
            await _responder.ReplyWith(outerDc.Context, CancelResponses._cancelConfirmed);

            // Cancel all in outer stack of component i.e. the stack the component belongs to
            return await outerDc.CancelAllDialogsAsync();
        }
    }
}

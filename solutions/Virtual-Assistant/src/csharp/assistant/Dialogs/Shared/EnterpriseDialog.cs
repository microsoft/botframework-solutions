// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Dialogs;

namespace VirtualAssistant
{
    public class EnterpriseDialog : InterruptableDialog
    {
        protected const string LuisResultKey = "LuisResult";

        // Fields
        private readonly BotServices _services;
        private readonly CancelResponses _responder = new CancelResponses();

        public EnterpriseDialog(BotServices botServices, string dialogId)
            : base(dialogId)
        {
            _services = botServices;

            AddDialog(new CancelDialog());
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            // check luis intent
            var luisService = _services.LuisServices["general"];
            var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
            var intent = luisResult.TopIntent().intent;

            // TODO - Evolve this pattern
            if (luisResult.TopIntent().score > 0.3)
            {
                // Add the luis result (intent and entities) for further processing in the derived dialog
                dc.Context.TurnState.Add(LuisResultKey, luisResult);

                switch (intent)
                {
                    case General.Intent.Cancel:
                        {
                            return await OnCancel(dc);
                        }

                    case General.Intent.Help:
                        {
                            return await OnHelp(dc);
                        }
                }
            }

            return InterruptionAction.NoAction;
        }

        protected virtual async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            if (dc.ActiveDialog.Id != nameof(CancelDialog))
            {
                // Don't start restart cancel dialog
                await dc.BeginDialogAsync(nameof(CancelDialog));

                // Signal that the dialog is waiting on user response
                return InterruptionAction.StartedDialog;
            }

            // Else, continue
            return InterruptionAction.NoAction;
        }

        protected virtual async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var view = new MainResponses();
            await view.ReplyWith(dc.Context, MainResponses.Help);

            // Signal the conversation was interrupted and should immediately continue
            return InterruptionAction.MessageSentToUser;
        }
    }
}

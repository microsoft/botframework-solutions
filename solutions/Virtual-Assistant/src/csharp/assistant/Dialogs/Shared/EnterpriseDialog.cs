// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Dialogs;
using VirtualAssistant.Dialogs.Main;

namespace VirtualAssistant.Dialogs.Shared
{
    public class EnterpriseDialog : InterruptableDialog
    {
        protected const string LuisResultKey = "LuisResult";

        // Fields
        private readonly BotServices _services;
        private readonly MainResponses _responder = new MainResponses();

        public EnterpriseDialog(BotServices botServices, string dialogId, IBotTelemetryClient telemetryClient)
            : base(dialogId, telemetryClient)
        {
            _services = botServices;
            TelemetryClient = telemetryClient;
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.LocaleConfigurations[locale];

            // check luis intent
            var luisService = localeConfig.LuisServices["general"];
            var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
            var intent = luisResult.TopIntent().intent;

            // TODO - Evolve this pattern
            if (luisResult.TopIntent().score > 0.5)
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
            // If user chose to cancel
            await _responder.ReplyWith(dc.Context, MainResponses.ResponseIds.Cancelled);

            // Cancel all in outer stack of component i.e. the stack the component belongs to
            await dc.CancelAllDialogsAsync();

            return InterruptionAction.StartedDialog;
        }

        protected virtual async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var view = new MainResponses();
            await view.ReplyWith(dc.Context, MainResponses.ResponseIds.Help);

            // Signal the conversation was interrupted and should immediately continue
            return InterruptionAction.MessageSentToUser;
        }
    }
}
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace CustomerSupportTemplate
{
    public class CustomerSupportDialog : InterruptibleDialog
    {
        protected const string LuisResultKey = "LuisResult";

        // Fields
        private readonly BotServices _services;

        private readonly CancelResponses _responder = new CancelResponses();

        public CustomerSupportDialog(BotServices botServices, string dialogId, IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            _services = botServices;
            TelemetryClient = telemetryClient;

            AddDialog(new CancelDialog(TelemetryClient));
            AddDialog(new EscalateDialog(botServices, TelemetryClient));
        }

        protected override async Task<InterruptionStatus> OnDialogInterruptionAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            if(dc.Context.Activity.Text != null)
            {
                // check luis intent
                _services.LuisServices.TryGetValue("general", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
                    var intent = luisResult.TopIntent().intent;

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
            }

            return InterruptionStatus.NoAction;
        }

        protected virtual async Task<InterruptionStatus> OnCancel(DialogContext dc)
        {
            if (dc.ActiveDialog.Id != nameof(CancelDialog))
            {
                // Don't restart cancel dialog
                await dc.BeginDialogAsync(nameof(CancelDialog));

                // Signal that the dialog is waiting on user response
                return InterruptionStatus.Waiting;
            }

            // Else, continue
            return InterruptionStatus.NoAction;
        }

        protected virtual async Task<InterruptionStatus> OnHelp(DialogContext dc)
        {
            var view = new MainResponses();
            await view.ReplyWith(dc.Context, MainResponses.Help);

            // Signal the conversation was interrupted and should immediately continue
            return InterruptionStatus.Interrupted;
        }
    }
}

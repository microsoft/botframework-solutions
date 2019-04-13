// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using System.Globalization;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder;
using VirtualAssistantSample.Responses.Cancel;
using VirtualAssistantSample.Responses.Main;
using VirtualAssistantSample.Services;

namespace VirtualAssistantSample.Dialogs
{
    public class EnterpriseDialog : InterruptableDialog
    {
        protected const string LuisResultKey = "LuisResult";

        // Fields
        private readonly BotServices _services;
        private readonly CancelResponses _responder = new CancelResponses();

        public EnterpriseDialog(string dialogId, BotServices botServices, IBotTelemetryClient telemetryClient)
            : base(dialogId, telemetryClient)
        {
            _services = botServices;

            AddDialog(new CancelDialog());
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            // Get cognitive models for locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var cognitiveModels = _services.CognitiveModelSets[locale];

            // check luis intent
            cognitiveModels.LuisServices.TryGetValue("general", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                General luisResult;
                if (dc.Context.TurnState.ContainsKey(LuisResultKey))
                {
                    luisResult = dc.Context.TurnState.Get<General>(LuisResultKey);
                }
                else
                {
                    luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);

                    // Add the luis result (intent and entities) for further processing in the derived dialog
                    dc.Context.TurnState.Add(LuisResultKey, luisResult);
                }

                var intent = luisResult.TopIntent().intent;

                // Only triggers interruption if confidence level is high
                if (luisResult.TopIntent().score > 0.5)
                {
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
            await view.ReplyWith(dc.Context, MainResponses.ResponseIds.Help);

            // Signal the conversation was interrupted and should immediately continue
            return InterruptionAction.MessageSentToUser;
        }
    }
}

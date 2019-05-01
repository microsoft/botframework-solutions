// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AutomotiveSkill.Models;
using AutomotiveSkill.Responses.Main;
using AutomotiveSkill.Responses.Shared;
using AutomotiveSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Schema;

namespace AutomotiveSkill.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotServices _services;
        private ResponseManager _responseManager;
        private ConversationState _conversationState;
        private IStatePropertyAccessor<AutomotiveSkillState> _stateAccessor;

        public MainDialog(
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
			VehicleSettingsDialog vehicleSettingsDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _services = services;
            _responseManager = responseManager;
            _conversationState = conversationState;
            TelemetryClient = telemetryClient;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<AutomotiveSkillState>(nameof(AutomotiveSkillState));

			// Register dialogs
			AddDialog(vehicleSettingsDialog ?? throw new ArgumentNullException(nameof(vehicleSettingsDialog)));
		}

		protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // send a greeting if we're in local mode
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(AutomotiveSkillMainResponses.WelcomeMessage));
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new AutomotiveSkillState());

            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.CognitiveModelSets[locale];

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("settings", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var turnResult = EndOfTurn;
                var result = await luisService.RecognizeAsync<Luis.VehicleSettingsLuis>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;

                // Update state with vehiclesettings luis result and entities
                state.AddRecognizerResult(result);

                // switch on general intents
                switch (intent)
                {
                    case VehicleSettingsLuis.Intent.VEHICLE_SETTINGS_CHANGE:
                    case VehicleSettingsLuis.Intent.VEHICLE_SETTINGS_DECLARATIVE:
                    case VehicleSettingsLuis.Intent.VEHICLE_SETTINGS_CHECK:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(VehicleSettingsDialog));
                            break;
                        }

                    case VehicleSettingsLuis.Intent.None:
                        {
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(AutomotiveSkillSharedResponses.DidntUnderstandMessage));
                            turnResult = new DialogTurnResult(DialogTurnStatus.Complete);

                            break;
                        }

                    default:
                        {
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(AutomotiveSkillMainResponses.FeatureNotAvailable));
                            turnResult = new DialogTurnResult(DialogTurnStatus.Complete);

                            break;
                        }
                }

                if (turnResult != EndOfTurn)
                {
                    await CompleteAsync(dc);
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = dc.Context.Activity.CreateReply();
            response.Type = ActivityTypes.EndOfConversation;

            await dc.Context.SendActivityAsync(response);
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (dc.Context.Activity.Name)
            {
                case SkillEvents.SkillBeginEventName:
                    {
                        var state = await _stateAccessor.GetAsync(dc.Context, () => new AutomotiveSkillState());

                        if (dc.Context.Activity.Value is Dictionary<string, object> userData)
                        {
                            // capture any user data sent to the skill from the parent here.
                        }

                        break;
                    }
            }
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                // get current activity locale
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = _services.CognitiveModelSets[locale];

                // check general luis intent
                localeConfig.LuisServices.TryGetValue("general", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
                    var topIntent = luisResult.TopIntent().intent;

                    // check intent
                    switch (topIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                result = await OnCancel(dc);
                                break;
                            }

                        case General.Intent.Help:
                            {
                                result = await OnHelp(dc);
                                break;
                            }
                    }
                }
            }

            return result;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            var response = _responseManager.GetResponse(AutomotiveSkillMainResponses.CancelMessage);
            await dc.Context.SendActivityAsync(response);

            await CompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(AutomotiveSkillMainResponses.HelpMessage));
            return InterruptionAction.MessageSentToUser;
        }
    }
}
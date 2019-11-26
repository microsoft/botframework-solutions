// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace AutomotiveSkill.Dialogs
{
    public class MainDialog : ActivityHandlerDialog
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

        protected override async Task OnMembersAddedAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // send a greeting if we're in local mode
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(AutomotiveSkillMainResponses.WelcomeMessage));
        }

        protected override async Task OnMessageActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new AutomotiveSkillState());

            // get current activity locale
            var localeConfig = _services.GetCognitiveModels();

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("Settings", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var result = await luisService.RecognizeAsync<Luis.SettingsLuis>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;

                // Update state with vehicle settings luis result and entities
                state.AddRecognizerResult(result);

                // switch on general intents
                switch (intent)
                {
                    case SettingsLuis.Intent.VEHICLE_SETTINGS_CHANGE:
                    case SettingsLuis.Intent.VEHICLE_SETTINGS_DECLARATIVE:
                    case SettingsLuis.Intent.VEHICLE_SETTINGS_CHECK:
                        {
                            await dc.BeginDialogAsync(nameof(VehicleSettingsDialog));
                            break;
                        }

                    case SettingsLuis.Intent.None:
                        {
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(AutomotiveSkillSharedResponses.DidntUnderstandMessage));
                            break;
                        }

                    default:
                        {
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(AutomotiveSkillMainResponses.FeatureNotAvailable));
                            break;
                        }
                }
            }
        }

        protected override async Task OnDialogCompleteAsync(DialogContext dc, object result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // workaround. if connect skill directly to teams, the following response does not work.
            if (dc.Context.Adapter is IRemoteUserTokenProvider remoteInvocationAdapter || Channel.GetChannelId(dc.Context) != Channels.Msteams)
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.Handoff;

                await dc.Context.SendActivityAsync(response);
            }

            await dc.EndDialogAsync(result);
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                // get current activity locale
                var localeConfig = _services.GetCognitiveModels();

                // check general luis intent
                localeConfig.LuisServices.TryGetValue("General", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
                    var topIntent = luisResult.TopIntent();

                    // check intent
                    if (topIntent.score > 0.5)
                    {
                        switch (topIntent.intent)
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
            }

            return result;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            var response = _responseManager.GetResponse(AutomotiveSkillMainResponses.CancelMessage);
            await dc.Context.SendActivityAsync(response);

            await OnDialogCompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.End;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(AutomotiveSkillMainResponses.HelpMessage));
            return InterruptionAction.Resume;
        }
    }
}
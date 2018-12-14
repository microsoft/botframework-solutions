﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AutomotiveSkill.Dialogs.Main.Resources;
using AutomotiveSkill.Dialogs.Shared.Resources;
using Luis;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;

namespace AutomotiveSkill
{
    public class MainDialog : RouterDialog
    {
        private bool _skillMode;
        private ISkillConfiguration _services;
        private UserState _userState;
        private IBotTelemetryClient _telemetryClient;
        private ConversationState _conversationState;
        private IServiceManager _serviceManager;
        private IHttpContextAccessor _httpContext;
        private IStatePropertyAccessor<AutomotiveSkillState> _stateAccessor;
        private AutomotiveSkillResponseBuilder _responseBuilder = new AutomotiveSkillResponseBuilder();

        public MainDialog(ISkillConfiguration services, ConversationState conversationState, UserState userState, IServiceManager serviceManager, IHttpContextAccessor httpContext, IBotTelemetryClient telemetryClient, bool skillMode)
            : base(nameof(MainDialog), telemetryClient)
        {
            _skillMode = skillMode;
            _services = services;
            _conversationState = conversationState;
            _userState = userState;
            _telemetryClient = telemetryClient;
            _serviceManager = serviceManager;
            _httpContext = httpContext;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<AutomotiveSkillState>(nameof(AutomotiveSkillState));

            // Register dialogs
            RegisterDialogs();
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_skillMode)
            {
                // send a greeting if we're in local mode
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(AutomotiveSkillMainResponses.WelcomeMessage));
            }
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new AutomotiveSkillState());

            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.LocaleConfigurations[locale];

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("settings", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var result = await luisService.RecognizeAsync<VehicleSettings>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;

                var skillOptions = new AutomotiveSkillDialogOptions
                {
                    SkillMode = _skillMode,
                };

                // switch on general intents
                switch (intent)
                {
                    case VehicleSettings.Intent.VEHICLE_SETTINGS_CHANGE:
                    case VehicleSettings.Intent.VEHICLE_SETTINGS_DECLARATIVE:
                    case VehicleSettings.Intent.VEHICLE_SETTINGS_CHECK:
                        await dc.BeginDialogAsync(nameof(VehicleSettingsDialog), skillOptions);

                        break;
                    case VehicleSettings.Intent.None:
                        {
                            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(AutomotiveSkillSharedResponses.DidntUnderstandMessage));
                            if (_skillMode)
                            {
                                await CompleteAsync(dc);
                            }

                            break;
                        }

                    default:
                        {
                            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(AutomotiveSkillMainResponses.FeatureNotAvailable));

                            if (_skillMode)
                            {
                                await CompleteAsync(dc);
                            }

                            break;
                        }
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_skillMode)
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.EndOfConversation;

                await dc.Context.SendActivityAsync(response);
            }
            else
            {
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(AutomotiveSkillSharedResponses.ActionEnded));
            }

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (dc.Context.Activity.Name)
            {
                case Events.SkillBeginEvent:
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
                // Update state with luis result and entities
                var state = await _stateAccessor.GetAsync(dc.Context, () => new AutomotiveSkillState());

                // get current activity locale
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = _services.LocaleConfigurations[locale];

                // Update state with vehiclesettings luis result and entities
                var vehicleSettingsLuisResult = await localeConfig.LuisServices["settings"].RecognizeAsync<VehicleSettings>(dc.Context, cancellationToken);
                state.VehicleSettingsLuisResult = vehicleSettingsLuisResult;

                // check luis intent
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
            await dc.BeginDialogAsync(nameof(CancelDialog));
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(AutomotiveSkillMainResponses.HelpMessage));
            return InterruptionAction.MessageSentToUser;
        }

        private void RegisterDialogs()
        {
            AddDialog(new CancelDialog());
            AddDialog(new VehicleSettingsDialog(_services, _stateAccessor, _serviceManager, _telemetryClient, _httpContext));
        }

        private class Events
        {
            public const string SkillBeginEvent = "skillBegin";
        }
    }
}

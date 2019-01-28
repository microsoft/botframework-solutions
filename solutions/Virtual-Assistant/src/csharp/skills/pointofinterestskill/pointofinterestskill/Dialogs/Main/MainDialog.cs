// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using PointOfInterestSkill.Dialogs.Cancel;
using PointOfInterestSkill.Dialogs.CancelRoute;
using PointOfInterestSkill.Dialogs.FindPointOfInterest;
using PointOfInterestSkill.Dialogs.Main.Resources;
using PointOfInterestSkill.Dialogs.Route;
using PointOfInterestSkill.Dialogs.Route.Resources;
using PointOfInterestSkill.Dialogs.Shared;
using PointOfInterestSkill.Dialogs.Shared.DialogOptions;
using PointOfInterestSkill.Dialogs.Shared.Resources;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.ServiceClients;

namespace PointOfInterestSkill.Dialogs.Main
{
    public class MainDialog : RouterDialog
    {
        private bool _skillMode;
        private SkillConfigurationBase _services;
        private UserState _userState;
        private ConversationState _conversationState;
        private IServiceManager _serviceManager;
        private IStatePropertyAccessor<PointOfInterestSkillState> _stateAccessor;
        private PointOfInterestResponseBuilder _responseBuilder = new PointOfInterestResponseBuilder();

        public MainDialog(
            SkillConfigurationBase services,
            ConversationState conversationState,
            UserState userState,
            IBotTelemetryClient telemetryClient,
            IServiceManager serviceManager,
            bool skillMode)
            : base(nameof(MainDialog), telemetryClient)
        {
            _skillMode = skillMode;
            _services = services;
            _userState = userState;
            _conversationState = conversationState;
            _serviceManager = serviceManager;
            TelemetryClient = telemetryClient;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<PointOfInterestSkillState>(nameof(PointOfInterestSkillState));

            // Register dialogs
            RegisterDialogs();
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_skillMode)
            {
                // send a greeting if we're in local mode
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(POIMainResponses.PointOfInterestWelcomeMessage));
            }
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var routeResult = EndOfTurn;

            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.LocaleConfigurations[locale];

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("pointofinterest", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var result = await luisService.RecognizeAsync<PointOfInterest>(dc, true, CancellationToken.None);

                var intent = result?.TopIntent().intent;

                var skillOptions = new PointOfInterestSkillDialogOptions
                {
                    SkillMode = _skillMode,
                };

                // switch on general intents
                switch (intent)
                {
                    case PointOfInterest.Intent.NAVIGATION_ROUTE_FROM_X_TO_Y:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(RouteDialog), skillOptions);
                            break;
                        }

                    case PointOfInterest.Intent.NAVIGATION_CANCEL_ROUTE:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(CancelRouteDialog), skillOptions);
                            break;
                        }

                    case PointOfInterest.Intent.NAVIGATION_FIND_POINTOFINTEREST:
                        {
                            routeResult = await dc.BeginDialogAsync(nameof(FindPointOfInterestDialog), skillOptions);
                            break;
                        }

                    case PointOfInterest.Intent.None:
                        {
                            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(POISharedResponses.DidntUnderstandMessage));
                            if (_skillMode)
                            {
                                routeResult = new DialogTurnResult(DialogTurnStatus.Complete);
                            }

                            break;
                        }

                    default:
                        {
                            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(POIMainResponses.FeatureNotAvailable));

                            if (_skillMode)
                            {
                                routeResult = new DialogTurnResult(DialogTurnStatus.Complete);
                            }

                            break;
                        }
                }
            }

            if (routeResult.Status == DialogTurnStatus.Complete)
            {
                await CompleteAsync(dc);
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
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(POISharedResponses.ActionEnded));
            }

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new PointOfInterestSkillState());

            switch (dc.Context.Activity.Name)
            {
                case Events.SkillBeginEvent:
                    {
                        if (dc.Context.Activity.Value is Dictionary<string, object> userData)
                        {
                            if (userData.TryGetValue("IPA.Location", out var location))
                            {
                                var coords = ((string)location).Split(',');
                                if (coords.Length == 2)
                                {
                                    if (double.TryParse(coords[0], out var lat) && double.TryParse(coords[1], out var lng))
                                    {
                                        var coordinates = new LatLng
                                        {
                                            Latitude = lat,
                                            Longitude = lng,
                                        };
                                        state.CurrentCoordinates = coordinates;
                                    }
                                }
                            }
                        }

                        break;
                    }

                case Events.Location:
                    {
                        // Test trigger with
                        // /event:{ "Name": "IPA.Location", "Value": "34.05222222222222,-118.2427777777777" }
                        var value = dc.Context.Activity.Value.ToString();

                        if (!string.IsNullOrEmpty(value))
                        {
                            var coords = value.Split(',');
                            if (coords.Length == 2)
                            {
                                if (double.TryParse(coords[0], out var lat) && double.TryParse(coords[1], out var lng))
                                {
                                    var coordinates = new LatLng
                                    {
                                        Latitude = lat,
                                        Longitude = lng,
                                    };
                                    state.CurrentCoordinates = coordinates;
                                }
                            }
                        }

                        break;
                    }

                case Events.ActiveLocation:
                    {
                        // Test trigger with...
                        var activeLocationName = dc.Context.Activity.Value.ToString();

                        // Set ActiveLocation if one w/ matching name is found in FoundLocations
                        var activeLocation = state.FoundLocations?.FirstOrDefault(x => x.Name.Contains(activeLocationName, StringComparison.InvariantCultureIgnoreCase));
                        if (activeLocation != null)
                        {
                            state.ActiveLocation = activeLocation;
                            state.FoundLocations = null;
                        }

                        // Activity should have text to trigger next intent, update Type & Route again
                        if (!string.IsNullOrEmpty(dc.Context.Activity.Text))
                        {
                            dc.Context.Activity.Type = ActivityTypes.Message;
                            await RouteAsync(dc);
                        }

                        break;
                    }

                case Events.ActiveRoute:
                    {
                        int.TryParse(dc.Context.Activity.Value.ToString(), out var routeId);
                        var activeRoute = state.FoundRoutes[routeId];
                        if (activeRoute != null)
                        {
                            state.ActiveRoute = activeRoute;
                            state.FoundRoutes = null;
                        }

                        var replyMessage = dc.Context.Activity.CreateReply(RouteResponses.SendingRouteDetails);
                        await dc.Context.SendActivityAsync(replyMessage);

                        // Send event with active route data
                        var replyEvent = dc.Context.Activity.CreateReply();
                        replyEvent.Type = ActivityTypes.Event;
                        replyEvent.Name = "ActiveRoute.Directions";
                        replyEvent.Value = state.ActiveRoute.Legs;
                        await dc.Context.SendActivityAsync(replyEvent);
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
                var localeConfig = _services.LocaleConfigurations[locale];

                // Update state with email luis result and entities
                var poiLuisResult = await localeConfig.LuisServices["pointofinterest"].RecognizeAsync<PointOfInterest>(dc.Context, cancellationToken);
                var state = await _stateAccessor.GetAsync(dc.Context, () => new PointOfInterestSkillState());
                state.LuisResult = poiLuisResult;

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

                    switch (topIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                result = await OnCancel(dc);
                                break;
                            }

                        case General.Intent.Help:
                            {
                                // result = await OnHelp(dc);
                                break;
                            }

                        case General.Intent.Logout:
                            {
                                result = await OnLogout(dc);
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
            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(POIMainResponses.HelpMessage));
            return InterruptionAction.MessageSentToUser;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
        {
            BotFrameworkAdapter adapter;
            var supported = dc.Context.Adapter is BotFrameworkAdapter;
            if (!supported)
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
            else
            {
                adapter = (BotFrameworkAdapter)dc.Context.Adapter;
            }

            await dc.CancelAllDialogsAsync();

            // Sign out user
            var tokens = await adapter.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
            foreach (var token in tokens)
            {
                await adapter.SignOutUserAsync(dc.Context, token.ConnectionName);
            }

            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(POIMainResponses.LogOut));

            return InterruptionAction.StartedDialog;
        }

        private void RegisterDialogs()
        {
            AddDialog(new RouteDialog(_services, _stateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new CancelRouteDialog(_services, _stateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new FindPointOfInterestDialog(_services, _stateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new CancelDialog(_stateAccessor, TelemetryClient));
        }

        public class Events
        {
            public const string ActiveLocation = "POI.ActiveLocation";
            public const string ActiveRoute = "POI.ActiveRoute";
            public const string Location = "IPA.Location";
            public const string SkillBeginEvent = "skillBegin";
        }
    }
}
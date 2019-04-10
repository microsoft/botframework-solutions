// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Schema;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Responses.Main;
using PointOfInterestSkill.Responses.Route;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;

namespace PointOfInterestSkill.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private UserState _userState;
        private ConversationState _conversationState;
        private IServiceManager _serviceManager;
        private IStatePropertyAccessor<PointOfInterestSkillState> _stateAccessor;
        private IHttpContextAccessor _httpContext;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext,
            IServiceManager serviceManager)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _responseManager = responseManager;
            _userState = userState;
            _conversationState = conversationState;
            _serviceManager = serviceManager;
            TelemetryClient = telemetryClient;
            _httpContext = httpContext;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<PointOfInterestSkillState>(nameof(PointOfInterestSkillState));

            // Register dialogs
            RegisterDialogs();
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // send a greeting if we're in local mode
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.PointOfInterestWelcomeMessage));
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.CognitiveModelSets[locale];

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("pointofinterest", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var turnResult = EndOfTurn;
                var result = await luisService.RecognizeAsync<PointOfInterestLuis>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;

                // switch on general intents
                switch (intent)
                {
                    case PointOfInterestLuis.Intent.NAVIGATION_ROUTE_FROM_X_TO_Y:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(RouteDialog));
                            break;
                        }

                    case PointOfInterestLuis.Intent.NAVIGATION_CANCEL_ROUTE:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(CancelRouteDialog));
                            break;
                        }

                    case PointOfInterestLuis.Intent.NAVIGATION_FIND_POINTOFINTEREST:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(FindPointOfInterestDialog));
                            break;
                        }

                    case PointOfInterestLuis.Intent.NAVIGATION_FIND_PARKING:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(FindParkingDialog));
                            break;
                        }

                    case PointOfInterestLuis.Intent.None:
                        {
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(POISharedResponses.DidntUnderstandMessage));
                            turnResult = new DialogTurnResult(DialogTurnStatus.Complete);

                            break;
                        }

                    default:
                        {
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.FeatureNotAvailable));
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
                        var activeLocation = state.LastFoundPointOfInterests?.FirstOrDefault(x => x.Name.Contains(activeLocationName, StringComparison.InvariantCultureIgnoreCase));
                        if (activeLocation != null)
                        {
                            state.Destination = activeLocation;
                            state.LastFoundPointOfInterests = null;
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

                        var replyMessage = _responseManager.GetResponse(RouteResponses.SendingRouteDetails);
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
                var localeConfig = _services.CognitiveModelSets[locale];

                // Update state with email luis result and entities
                var poiLuisResult = await localeConfig.LuisServices["pointofinterest"].RecognizeAsync<PointOfInterestLuis>(dc.Context, cancellationToken);
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
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.CancelMessage));
            await CompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.HelpMessage));
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

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.LogOut));

            return InterruptionAction.StartedDialog;
        }

        private void RegisterDialogs()
        {
            AddDialog(new RouteDialog(_settings, _services, _responseManager, _stateAccessor, _serviceManager, TelemetryClient, _httpContext));
            AddDialog(new CancelRouteDialog(_settings, _services, _responseManager, _stateAccessor, _serviceManager, TelemetryClient, _httpContext));
            AddDialog(new FindPointOfInterestDialog(_settings, _services, _responseManager, _stateAccessor, _serviceManager, TelemetryClient, _httpContext));
            AddDialog(new FindParkingDialog(_settings, _services, _responseManager, _stateAccessor, _serviceManager, TelemetryClient, _httpContext));
        }

        public class Events
        {
            public const string ActiveLocation = "IPA.ActiveLocation";
            public const string ActiveRoute = "IPA.ActiveRoute";
            public const string Location = "IPA.Location";
            public const string SkillBeginEvent = "skillBegin";
        }
    }
}
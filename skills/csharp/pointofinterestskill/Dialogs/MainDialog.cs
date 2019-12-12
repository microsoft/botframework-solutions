// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Responses.Main;
using PointOfInterestSkill.Responses.Route;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using SkillServiceLibrary.Models;

namespace PointOfInterestSkill.Dialogs
{
    // Dialog providing activity routing and message/event processing.
    public class MainDialog : ActivityHandlerDialog
    {
        private BotServices _services;
        private ResponseManager _responseManager;
        private UserState _userState;
        private ConversationState _conversationState;
        private IStatePropertyAccessor<PointOfInterestSkillState> _stateAccessor;

        public MainDialog(
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            RouteDialog routeDialog,
            CancelRouteDialog cancelRouteDialog,
            FindPointOfInterestDialog findPointOfInterestDialog,
            FindParkingDialog findParkingDialog,
            GetDirectionsDialog getDirectionsDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _services = services;
            _responseManager = responseManager;
            _userState = userState;
            _conversationState = conversationState;
            TelemetryClient = telemetryClient;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<PointOfInterestSkillState>(nameof(PointOfInterestSkillState));

            // Register dialogs
            AddDialog(routeDialog ?? throw new ArgumentNullException(nameof(routeDialog)));
            AddDialog(cancelRouteDialog ?? throw new ArgumentNullException(nameof(cancelRouteDialog)));
            AddDialog(findPointOfInterestDialog ?? throw new ArgumentNullException(nameof(findPointOfInterestDialog)));
            AddDialog(findParkingDialog ?? throw new ArgumentNullException(nameof(findParkingDialog)));
            AddDialog(getDirectionsDialog ?? throw new ArgumentNullException(nameof(getDirectionsDialog)));
        }

        // Runs on every turn of the conversation.
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("PointOfInterest", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<PointOfInterestLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.POILuisResultKey, skillResult);
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResultKey, generalResult);
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Runs when the dialog stack is empty, and a new member is added to the conversation. Can be used to send an introduction activity.
        protected override async Task OnMembersAddedAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // send a greeting if we're in local mode
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.PointOfInterestWelcomeMessage));
        }

        // Runs when the dialog stack is empty, and a new message activity comes in.
        protected override async Task OnMessageActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await PopulateStateFromSemanticAction(dc.Context);

            var result = dc.Context.TurnState.Get<PointOfInterestLuis>(StateProperties.POILuisResultKey);
            var intent = result?.TopIntent().intent;

            if (intent != PointOfInterestLuis.Intent.None)
            {
                var state = await _stateAccessor.GetAsync(dc.Context, () => new PointOfInterestSkillState());
                await DigestLuisResult(dc, result);
            }

            // switch on General intents
            switch (intent)
            {
                case PointOfInterestLuis.Intent.GetDirections:
                    {
                        await dc.BeginDialogAsync(nameof(GetDirectionsDialog));
                        break;
                    }

                case PointOfInterestLuis.Intent.FindPointOfInterest:
                    {
                        await dc.BeginDialogAsync(nameof(FindPointOfInterestDialog));
                        break;
                    }

                case PointOfInterestLuis.Intent.FindParking:
                    {
                        await dc.BeginDialogAsync(nameof(FindParkingDialog));
                        break;
                    }

                case PointOfInterestLuis.Intent.None:
                    {
                        await dc.Context.SendActivityAsync(_responseManager.GetResponse(POISharedResponses.DidntUnderstandMessage));
                        break;
                    }

                default:
                    {
                        await dc.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.FeatureNotAvailable));
                        break;
                    }
            }
        }

        // Runs when the dialog stack completes.
        protected override async Task OnDialogCompleteAsync(DialogContext dc, object result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // workaround. if connect skill directly to teams, the following response does not work.
            if (!dc.SuppressCompletionMessage() && (dc.Context.Adapter is IRemoteUserTokenProvider remoteInvocationAdapter || Channel.GetChannelId(dc.Context) != Channels.Msteams))
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.Handoff;
                await dc.Context.SendActivityAsync(response);
            }

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        // Runs when a new event activity comes in.
        protected override async Task OnEventActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var ev = dc.Context.Activity.AsEventActivity();
            var value = ev.Value?.ToString();

            var state = await _stateAccessor.GetAsync(dc.Context, () => new PointOfInterestSkillState());

            switch (ev.Name)
            {
                case Events.Location:
                    {
                        dc.SuppressCompletionMessage(true);

                        // Test trigger with
                        // /event:{ "Name": "Location", "Value": "34.05222222222222,-118.2427777777777" }
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

                case TokenEvents.TokenResponseEventName:
                    {
                        // Forward the token response activity to the dialog waiting on the stack.
                        await dc.ContinueDialogAsync();
                        break;
                    }

                default:
                    {
                        await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                        break;
                    }
            }
        }

        // Runs when an activity with an unknown type is received.
        protected override async Task OnUnhandledActivityTypeAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            await innerDc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown activity was received but not processed."));
        }

        // Runs on every turn of the conversation to check if the conversation should be interrupted.
        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(dc.Context.Activity.Text))
            {
                var luisResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var state = await _stateAccessor.GetAsync(dc.Context, () => new PointOfInterestSkillState());
                var topIntent = luisResult.TopIntent();

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

        private async Task PopulateStateFromSemanticAction(ITurnContext context)
        {
            var activity = context.Activity;
            var semanticAction = activity.SemanticAction;
            if (semanticAction != null && semanticAction.Entities.ContainsKey(StateProperties.LocationKey))
            {
                var location = semanticAction.Entities[StateProperties.LocationKey];
                var locationObj = location.Properties[StateProperties.LocationKey].ToString();

                var coords = locationObj.Split(',');
                if (coords.Length == 2)
                {
                    if (double.TryParse(coords[0], out var lat) && double.TryParse(coords[1], out var lng))
                    {
                        var coordinates = new LatLng
                        {
                            Latitude = lat,
                            Longitude = lng,
                        };

                        var state = await _stateAccessor.GetAsync(context, () => new PointOfInterestSkillState());
                        state.CurrentCoordinates = coordinates;
                    }
                }
            }
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.CancelMessage));
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.End;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.HelpMessage));
            return InterruptionAction.Resume;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
        {
            IUserTokenProvider tokenProvider;
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (supported)
            {
                tokenProvider = (IUserTokenProvider)dc.Context.Adapter;

                // Sign out user
                var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
                foreach (var token in tokens)
                {
                    await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName);
                }

                // Cancel all active dialogs
                await dc.CancelAllDialogsAsync();
            }
            else
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(POIMainResponses.LogOut));

            return InterruptionAction.End;
        }

        private async Task DigestLuisResult(DialogContext dc, PointOfInterestLuis luisResult)
        {
            try
            {
                var state = await _stateAccessor.GetAsync(dc.Context, () => new PointOfInterestSkillState());

                if (luisResult != null)
                {
                    state.Clear();

                    var entities = luisResult.Entities;

                    // TODO since we can only search one per search, only the 1st one is considered
                    if (entities.Keyword != null)
                    {
                        if (entities._instance.KeywordCategory == null || !entities._instance.KeywordCategory.Any(c => c.Text.Equals(entities.Keyword[0], StringComparison.InvariantCultureIgnoreCase)))
                        {
                            state.Keyword = entities.Keyword[0];
                        }
                    }

                    // TODO if keyword exists and category exists, whether keyword contains category or a keyword of some category. We will ignore category in these two cases
                    if (string.IsNullOrEmpty(state.Keyword) && entities._instance.KeywordCategory != null)
                    {
                        state.Category = entities._instance.KeywordCategory[0].Text;
                    }

                    if (entities.Address != null)
                    {
                        state.Address = string.Join(" ", entities.Address);
                    }
                    else
                    {
                        // ADDRESS overwrites geographyV2
                        var sb = new StringBuilder();

                        if (entities.geographyV2 != null)
                        {
                            sb.AppendJoin(" ", entities.geographyV2.Select(geography => geography.Location));
                        }

                        if (sb.Length > 0)
                        {
                            state.Address = sb.ToString();
                        }
                    }

                    // TODO only first is used now
                    if (entities.RouteDescription != null)
                    {
                        state.RouteType = entities.RouteDescription[0][0];
                    }

                    if (entities.PoiDescription != null)
                    {
                        state.PoiType = entities.PoiDescription[0][0];
                    }

                    // TODO unused
                    if (entities.number != null)
                    {
                        try
                        {
                            var value = entities.number[0];
                            if (Math.Abs(value - (int)value) < double.Epsilon)
                            {
                                state.UserSelectIndex = (int)value - 1;
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }
            }
            catch
            {
                // put log here
            }
        }

        public class Events
        {
            public const string Location = "Location";
        }
    }
}
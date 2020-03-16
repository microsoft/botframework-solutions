// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Responses.Route;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Utilities;
using SkillServiceLibrary.Models;

namespace PointOfInterestSkill.Dialogs
{
    public class RouteDialog : PointOfInterestDialogBase
    {
        public RouteDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(nameof(RouteDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, httpContext)
        {
            TelemetryClient = telemetryClient;

            var checkCurrentLocation = new WaterfallStep[]
            {
                CheckForCurrentCoordinatesBeforeRoute,
                ConfirmCurrentLocation,
                ProcessCurrentLocationSelection,
                RouteToFindPointOfInterestDialog,
            };

            var findRouteToActiveLocation = new WaterfallStep[]
            {
                GetRoutesToDestination,
                ResponseToStartRoutePrompt,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CheckForCurrentLocation, checkCurrentLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindRouteToActiveLocation, findRouteToActiveLocation) { TelemetryClient = telemetryClient });
            AddDialog(new ConfirmPrompt(Actions.StartNavigationPrompt, ValidateStartNavigationPrompt) { Style = ListStyle.None });

            // Set starting dialog for component
            InitialDialogId = Actions.CheckForCurrentLocation;
        }

        /// <summary>
        /// Check for the current coordinates and if missing, prompt user.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        public async Task<DialogTurnResult> CheckForCurrentCoordinatesBeforeRoute(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
            var hasCurrentCoordinates = state.CheckForValidCurrentCoordinates();

            if (hasCurrentCoordinates)
            {
                return await sc.ReplaceDialogAsync(Actions.FindRouteToActiveLocation);
            }

            return await sc.PromptAsync(Actions.CurrentLocationPrompt, new PromptOptions { Prompt = ResponseManager.GetResponse(POISharedResponses.PromptForCurrentLocation) });
        }

        /// <summary>
        /// Replaces the active dialog with the FindPointOfInterestBeforeRoute waterfall dialog.
        /// </summary>
        /// <param name="sc">WaterfallStepContext.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>DialogTurnResult.</returns>
        public async Task<DialogTurnResult> RouteToFindPointOfInterestDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.ReplaceDialogAsync(Actions.FindRouteToActiveLocation);
        }

        public async Task<DialogTurnResult> CheckIfActiveRouteExists(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.ActiveRoute != null)
                {
                    await sc.EndDialogAsync(true);
                    return await sc.BeginDialogAsync(Actions.FindAlongRoute);
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> CheckIfFoundLocationExists(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.LastFoundPointOfInterests == null)
                {
                    return await sc.NextAsync();
                }

                if (!string.IsNullOrEmpty(state.Keyword))
                {
                    // Set ActiveLocation if one w/ matching name is found in FoundLocations
                    var activeLocation = state.LastFoundPointOfInterests?.FirstOrDefault(x => x.Name.Contains(state.Keyword, StringComparison.InvariantCultureIgnoreCase));
                    if (activeLocation != null)
                    {
                        state.Destination = activeLocation;
                        state.LastFoundPointOfInterests = null;
                    }
                }

                if (!string.IsNullOrEmpty(state.Address) && state.LastFoundPointOfInterests != null)
                {
                    // Set ActiveLocation if one w/ matching address is found in FoundLocations
                    var activeLocation = state.LastFoundPointOfInterests?.FirstOrDefault(x => x.Address.Contains(state.Address, StringComparison.InvariantCultureIgnoreCase));
                    if (activeLocation != null)
                    {
                        state.Destination = activeLocation;
                        state.LastFoundPointOfInterests = null;
                    }
                }

                if (state.UserSelectIndex >= 0 && state.UserSelectIndex < state.LastFoundPointOfInterests.Count)
                {
                    // Set ActiveLocation if one w/ matching address is found in FoundLocations
                    var activeLocation = state.LastFoundPointOfInterests?[state.UserSelectIndex];
                    if (activeLocation != null)
                    {
                        state.Destination = activeLocation;
                        state.LastFoundPointOfInterests = null;
                    }
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> CheckIfDestinationExists(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.Destination == null)
                {
                    await sc.EndDialogAsync(true);
                    return await sc.BeginDialogAsync(Actions.CheckForCurrentLocation);
                }

                return await sc.BeginDialogAsync(Actions.FindRouteToActiveLocation);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> GetRoutesToDestination(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var service = ServiceManager.InitRoutingMapsService(Settings);
                var routeDirections = new RouteDirections();
                var cards = new List<Card>();

                if (state.Destination == null || !state.CheckForValidCurrentCoordinates())
                {
                    // should not happen
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(RouteResponses.MissingActiveLocationErrorMessage));
                    return await sc.EndDialogAsync();
                }

                if (!string.IsNullOrEmpty(state.RouteType))
                {
                    routeDirections = await service.GetRouteDirectionsToDestinationAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Destination.Geolocation.Latitude, state.Destination.Geolocation.Longitude, state.RouteType);

                    cards = await GetRouteDirectionsViewCards(sc, routeDirections, service);
                }
                else
                {
                    routeDirections = await service.GetRouteDirectionsToDestinationAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Destination.Geolocation.Latitude, state.Destination.Geolocation.Longitude);

                    cards = await GetRouteDirectionsViewCards(sc, routeDirections, service);
                }

                if (cards.Count() == 0)
                {
                    var replyMessage = ResponseManager.GetResponse(POISharedResponses.NoRouteFound);
                    await sc.Context.SendActivityAsync(replyMessage);

                    return await sc.EndDialogAsync();
                }
                else if (cards.Count() == 1)
                {
                    var options = new PromptOptions { Prompt = ResponseManager.GetCardResponse(cards[0]) };

                    if (state.DestinationActionType == DestinationActionType.ShowDirectionsThenStartNavigation)
                    {
                        await sc.Context.SendActivityAsync(options.Prompt);
                        return await sc.NextAsync(true);
                    }

                    return await sc.PromptAsync(Actions.StartNavigationPrompt, options);
                }
                else
                {
                    var options = GetRoutesPrompt(POISharedResponses.MultipleRoutesFound, cards);

                    if (state.DestinationActionType == DestinationActionType.ShowDirectionsThenStartNavigation)
                    {
                        await sc.Context.SendActivityAsync(options.Prompt);
                        return await sc.NextAsync(true);
                    }

                    return await sc.PromptAsync(Actions.StartNavigationPrompt, options);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ResponseToStartRoutePrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                SingleDestinationResponse response = null;

                if (sc.Result is bool && (bool)sc.Result)
                {
                    // TODO do not care multiple routes
                    var activeRoute = state.FoundRoutes[0];

                    if (activeRoute != null)
                    {
                        state.ActiveRoute = activeRoute;
                        state.FoundRoutes = null;
                    }

                    if (SupportOpenDefaultAppReply(sc.Context))
                    {
                        await sc.Context.SendActivityAsync(CreateOpenDefaultAppReply(sc.Context.Activity, state.Destination, OpenDefaultAppType.Map));
                    }

                    response = ConvertToResponse(state.Destination);
                }
                else
                {
                    var replyMessage = ResponseManager.GetResponse(RouteResponses.AskAboutRouteLater);
                    await sc.Context.SendActivityAsync(replyMessage);
                }

                return await sc.EndDialogAsync(response);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private Task<bool> ValidateStartNavigationPrompt(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded && promptContext.Recognized.Value == false)
            {
                return Task.FromResult(true);
            }

            if (promptContext.Context.Activity.Type == ActivityTypes.Message)
            {
                var message = promptContext.Context.Activity.AsMessageActivity();
                if (message.Text.Contains(PointOfInterestSharedStrings.START, StringComparison.InvariantCultureIgnoreCase))
                {
                    promptContext.Recognized.Value = true;
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        private PromptOptions GetRoutesPrompt(string prompt, List<Card> cards)
        {
            var options = new PromptOptions()
            {
                Choices = new List<Choice>(),
            };

            for (var i = 0; i < cards.Count; ++i)
            {
                // Simple distinction
                var promptReplacements = new StringDictionary
                    {
                        { "Id", (i + 1).ToString() },
                    };
                var suggestedActionValue = ResponseManager.GetResponse(RouteResponses.RouteSuggestedActionName, promptReplacements).Text;

                var choice = new Choice()
                {
                    Value = suggestedActionValue,
                };
                options.Choices.Add(choice);

                (cards[i].Data as RouteDirectionsModel).SubmitText = suggestedActionValue;
            }

            options.Prompt = cards == null ? ResponseManager.GetResponse(prompt) : ResponseManager.GetCardResponse(prompt, cards);
            options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options.Prompt);

            return options;
        }
    }
}
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
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Responses.Route;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Utilities;

namespace PointOfInterestSkill.Dialogs
{
    public class RouteDialog : PointOfInterestDialogBase
    {
        public RouteDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(nameof(RouteDialog), settings, services, conversationState, serviceManager, telemetryClient, httpContext)
        {
            TelemetryClient = telemetryClient;

            var checkCurrentLocation = new WaterfallStep[]
            {
                CheckForCurrentCoordinatesBeforeFindPointOfInterestBeforeRoute,
                ConfirmCurrentLocation,
                ProcessCurrentLocationSelection,
                RouteToFindPointOfInterestBeforeRouteDialog
            };

            var checkForActiveRouteAndLocation = new WaterfallStep[]
            {
                CheckIfActiveRouteExists,
                CheckIfFoundLocationExists,
                CheckIfDestinationExists,
            };

            var findRouteToActiveLocation = new WaterfallStep[]
            {
                GetRoutesToDestination,
                ResponseToStartRoutePrompt,
            };

            var findAlongRoute = new WaterfallStep[]
            {
                GetPointOfInterestLocations,
                ProcessPointOfInterestSelection,
                GetRoutesToDestination,
                ResponseToStartRoutePrompt,
            };

            var findPointOfInterest = new WaterfallStep[]
            {
                GetPointOfInterestLocations,
                ProcessPointOfInterestSelection,
                GetRoutesToDestination,
                ResponseToStartRoutePrompt,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CheckForCurrentLocation, checkCurrentLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.GetActiveRoute, checkForActiveRouteAndLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindAlongRoute, findAlongRoute) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindRouteToActiveLocation, findRouteToActiveLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindPointOfInterestBeforeRoute, findPointOfInterest) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.GetActiveRoute;
        }

        /// <summary>
        /// Check for the current coordinates and if missing, prompt user.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        public async Task<DialogTurnResult> CheckForCurrentCoordinatesBeforeFindPointOfInterestBeforeRoute(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
            var hasCurrentCoordinates = state.CheckForValidCurrentCoordinates();

            if (hasCurrentCoordinates)
            {
                return await sc.ReplaceDialogAsync(Actions.FindPointOfInterestBeforeRoute);
            }

            return await sc.PromptAsync(Actions.CurrentLocationPrompt, new PromptOptions { Prompt = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[PromptForCurrentLocation]") as Activity });
        }

        /// <summary>
        /// Replaces the active dialog with the FindPointOfInterestBeforeRoute waterfall dialog.
        /// </summary>
        /// <param name="sc">WaterfallStepContext.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>DialogTurnResult.</returns>
        public async Task<DialogTurnResult> RouteToFindPointOfInterestBeforeRouteDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);

            return await sc.ReplaceDialogAsync(Actions.FindPointOfInterestBeforeRoute);
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

                state.CheckForValidCurrentCoordinates();

                if (state.Destination == null)
                {
                    // No ActiveLocation found
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[MissingActiveLocationErrorMessage]") as Activity });
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
                    var replyMessage = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[NoLocationsFound]");
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else if (cards.Count() == 1)
                {
                    await sc.Context.SendActivityAsync(await LGHelper.GenerateAdaptiveCardAsync(_lgMultiLangEngine, sc.Context,  "[SingleRouteFound]", null, cards));

                    return await sc.NextAsync(true);
                }
                else
                {
                    var options = await GetRoutesPrompt("[MultipleRoutesFound]", cards, sc.Context);

                    // Workaround. In teams, HeroCard will be used for prompt and adaptive card could not be shown. So send them separatly
                    if (Channel.GetChannelId(sc.Context) == Channels.Msteams)
                    {
                        await sc.Context.SendActivityAsync(options.Prompt);
                        options.Prompt = null;
                    }

                    return await sc.PromptAsync(Actions.SelectPointOfInterestPrompt, options);
                }

                state.ClearLuisResults();

                return await sc.EndDialogAsync();
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

                if ((sc.Result is bool && (bool)sc.Result) || sc.Result is FoundChoice)
                {
                    var activeRoute = state.FoundRoutes[0];
                    if (sc.Result is FoundChoice)
                    {
                        activeRoute = state.FoundRoutes[(sc.Result as FoundChoice).Index];
                    }

                    if (activeRoute != null)
                    {
                        state.ActiveRoute = activeRoute;
                        state.FoundRoutes = null;
                    }

                    var replyMessage = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[SendingRouteDetails]");
                    await sc.Context.SendActivityAsync(replyMessage);

                    // workaround. if connect skill directly to teams, the following response does not work.
                    if (sc.Context.Adapter is IRemoteUserTokenProvider remoteInvocationAdapter || Channel.GetChannelId(sc.Context) != Channels.Msteams)
                    {
                        await sc.Context.SendActivityAsync(CreateOpenDefaultAppReply(sc.Context.Activity, state.Destination));
                    }
                }
                else
                {
                    var replyMessage = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[AskAboutRouteLater]");
                    await sc.Context.SendActivityAsync(replyMessage);
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<PromptOptions> GetRoutesPrompt(string prompt, List<Card> cards, ITurnContext context)
        {
            var options = new PromptOptions()
            {
                Choices = new List<Choice>(),
            };

            for (var i = 0; i < cards.Count; ++i)
            {
                // Simple distinction
                var promptReplacements = new
                {
                    input = new
                    {
                        Id = (i + 1).ToString()
                    }
                };
                var suggestedActionValue = await _lgMultiLangEngine.Generate(context, "[RouteSuggestedActionName]", promptReplacements);

                var choice = new Choice()
                {
                    Value = suggestedActionValue,
                };
                options.Choices.Add(choice);

                (cards[i].Data as RouteDirectionsModel).SubmitText = suggestedActionValue;
            }

            options.Prompt = (cards == null ? await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, context, prompt) : await LGHelper.GenerateAdaptiveCardAsync(_lgMultiLangEngine, context, prompt, null, cards)) as Activity;
            options.Prompt.Speak = SpeechUtility.ListToSpeechReadyString(options.Prompt);

            return options;
        }
    }
}
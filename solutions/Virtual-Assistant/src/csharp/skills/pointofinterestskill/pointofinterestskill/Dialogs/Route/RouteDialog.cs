// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using PointOfInterestSkill.Dialogs.Route.Resources;
using PointOfInterestSkill.Dialogs.Shared;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.ServiceClients;
using Actions = PointOfInterestSkill.Dialogs.Shared.Actions;

namespace PointOfInterestSkill.Dialogs.Route
{
    public class RouteDialog : PointOfInterestSkillDialog
    {
        public RouteDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(nameof(RouteDialog), services, responseManager, accessor, serviceManager, telemetryClient, httpContext)
        {
            TelemetryClient = telemetryClient;

            var checkForActiveRouteAndLocation = new WaterfallStep[]
            {
                CheckIfActiveRouteExists,
                CheckIfFoundLocationExists,
                CheckIfActiveLocationExists,
            };

            var findRouteToActiveLocation = new WaterfallStep[]
            {
                GetRoutesToActiveLocation,
                ResponseToStartRoutePrompt,
            };

            var findAlongRoute = new WaterfallStep[]
            {
                GetPointOfInterestLocations,
                ResponseToGetRoutePrompt,
            };

            var findPointOfInterest = new WaterfallStep[]
            {
                GetPointOfInterestLocations,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.GetActiveRoute, checkForActiveRouteAndLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindAlongRoute, findAlongRoute) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindRouteToActiveLocation, findRouteToActiveLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindPointOfInterest, findPointOfInterest) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.GetActiveRoute;
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
                    var activeLocation = state.LastFoundPointOfInterests?.FirstOrDefault(x => x.City.Contains(state.Address, StringComparison.InvariantCultureIgnoreCase));
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

        public async Task<DialogTurnResult> CheckIfActiveLocationExists(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.Destination == null)
                {
                    await sc.EndDialogAsync(true);
                    return await sc.BeginDialogAsync(Actions.FindPointOfInterest);
                }

                return await sc.BeginDialogAsync(Actions.FindRouteToActiveLocation);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> GetRoutesToActiveLocation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var service = ServiceManager.InitRoutingMapsService(Services);
                var routeDirections = new RouteDirections();

                state.CheckForValidCurrentCoordinates();

                if (state.Destination == null)
                {
                    // No ActiveLocation found
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(RouteResponses.MissingActiveLocationErrorMessage) });
                }

                if (!string.IsNullOrEmpty(state.RouteType))
                {
                    routeDirections = await service.GetRouteDirectionsToDestinationAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Destination.Geolocation.Latitude, state.Destination.Geolocation.Longitude, state.RouteType);

                    await GetRouteDirectionsViewCards(sc, routeDirections);
                }
                else
                {
                    routeDirections = await service.GetRouteDirectionsToDestinationAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Destination.Geolocation.Latitude, state.Destination.Geolocation.Longitude);

                    await GetRouteDirectionsViewCards(sc, routeDirections);
                }

                if (routeDirections?.Routes?.ToList().Count == 1)
                {
                    return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions { Prompt = ResponseManager.GetResponse(RouteResponses.PromptToStartRoute) });
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

                if ((bool)sc.Result)
                {
                    var activeRoute = state.FoundRoutes.Single();
                    if (activeRoute != null)
                    {
                        state.ActiveRoute = activeRoute;
                        state.FoundRoutes = null;
                    }

                    var replyMessage = ResponseManager.GetResponse(RouteResponses.SendingRouteDetails);
                    await sc.Context.SendActivityAsync(replyMessage);

                    // Send event with active route data
                    var replyEvent = sc.Context.Activity.CreateReply();
                    replyEvent.Type = ActivityTypes.Event;
                    replyEvent.Name = "ActiveRoute.Directions";

                    var eventPayload = new DirectionsEventResponse
                    {
                        Destination = state.Destination,
                        Route = state.ActiveRoute
                    };
                    replyEvent.Value = eventPayload;

                    await sc.Context.SendActivityAsync(replyEvent);
                }
                else
                {
                    var replyMessage = ResponseManager.GetResponse(RouteResponses.AskAboutRouteLater);
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
    }
}
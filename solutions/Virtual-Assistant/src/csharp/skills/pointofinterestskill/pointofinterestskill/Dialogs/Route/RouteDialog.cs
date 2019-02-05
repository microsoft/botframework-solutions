using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using PointOfInterestSkill.Dialogs.Route.Resources;
using PointOfInterestSkill.Dialogs.Shared;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.ServiceClients;
using Action = PointOfInterestSkill.Dialogs.Shared.Action;

namespace PointOfInterestSkill.Dialogs.Route
{
    public class RouteDialog : PointOfInterestSkillDialog
    {
        public RouteDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(RouteDialog), services, responseManager, accessor, serviceManager, telemetryClient)
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
            AddDialog(new WaterfallDialog(Action.GetActiveRoute, checkForActiveRouteAndLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.FindAlongRoute, findAlongRoute) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.FindRouteToActiveLocation, findRouteToActiveLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.FindPointOfInterest, findPointOfInterest) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Action.GetActiveRoute;
        }

        public async Task<DialogTurnResult> CheckIfActiveRouteExists(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.ActiveRoute != null)
                {
                    await sc.EndDialogAsync(true);
                    return await sc.BeginDialogAsync(Action.FindAlongRoute);
                }

                return await sc.NextAsync();
            }
            catch
            {
                await HandleDialogException(sc);
                throw;
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

                if (!string.IsNullOrEmpty(state.SearchText))
                {
                    // Set ActiveLocation if one w/ matching name is found in FoundLocations
                    var activeLocation = state.LastFoundPointOfInterests?.FirstOrDefault(x => x.Name.Contains(state.SearchText, StringComparison.InvariantCultureIgnoreCase));
                    if (activeLocation != null)
                    {
                        state.ActiveLocation = activeLocation;
                        state.LastFoundPointOfInterests = null;
                    }
                }

                if (!string.IsNullOrEmpty(state.SearchAddress) && state.LastFoundPointOfInterests != null)
                {
                    // Set ActiveLocation if one w/ matching address is found in FoundLocations
                    var activeLocation = state.LastFoundPointOfInterests?.FirstOrDefault(x => x.City.Contains(state.SearchAddress, StringComparison.InvariantCultureIgnoreCase));
                    if (activeLocation != null)
                    {
                        state.ActiveLocation = activeLocation;
                        state.LastFoundPointOfInterests = null;
                    }
                }

                if (state.UserSelectIndex >= 0 && state.UserSelectIndex < state.LastFoundPointOfInterests.Count)
                {
                    // Set ActiveLocation if one w/ matching address is found in FoundLocations
                    var activeLocation = state.LastFoundPointOfInterests?[state.UserSelectIndex];
                    if (activeLocation != null)
                    {
                        state.ActiveLocation = activeLocation;
                        state.LastFoundPointOfInterests = null;
                    }
                }

                return await sc.NextAsync();
            }
            catch
            {
                await HandleDialogException(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> CheckIfActiveLocationExists(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.ActiveLocation == null)
                {
                    await sc.EndDialogAsync(true);
                    return await sc.BeginDialogAsync(Action.FindPointOfInterest);
                }

                return await sc.BeginDialogAsync(Action.FindRouteToActiveLocation);
            }
            catch
            {
                await HandleDialogException(sc);
                throw;
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

                if (state.ActiveLocation == null)
                {
                    // No ActiveLocation found
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(RouteResponses.MissingActiveLocationErrorMessage) });
                }

                if (!string.IsNullOrEmpty(state.SearchDescriptor))
                {
                    routeDirections = await service.GetRouteDirectionsAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.ActiveLocation.Geolocation.Latitude, state.ActiveLocation.Geolocation.Longitude, state.SearchDescriptor);

                    await GetRouteDirectionsViewCards(sc, routeDirections);
                }
                else
                {
                    routeDirections = await service.GetRouteDirectionsAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.ActiveLocation.Geolocation.Latitude, state.ActiveLocation.Geolocation.Longitude);

                    await GetRouteDirectionsViewCards(sc, routeDirections);
                }

                if (routeDirections?.Routes?.ToList().Count == 1)
                {
                    return await sc.PromptAsync(Action.ConfirmPrompt, new PromptOptions { Prompt = ResponseManager.GetResponse(RouteResponses.PromptToStartRoute) });
                }

                state.ClearLuisResults();

                return await sc.EndDialogAsync();
            }
            catch
            {
                await HandleDialogException(sc);
                throw;
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
                        Destination = state.ActiveLocation,
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
            catch
            {
                await HandleDialogException(sc);
                throw;
            }
        }
    }
}
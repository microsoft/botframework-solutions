using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
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
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(RouteDialog), services, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

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

                if (!string.IsNullOrEmpty(state.Keyword))
                {
                    // Set Destionation if one w/ matching name is found in FoundLocations
                    var destination = state.LastFoundPointOfInterests?.FirstOrDefault(x => x.Name.Contains(state.Keyword, StringComparison.InvariantCultureIgnoreCase));
                    if (destination != null)
                    {
                        state.Destination = destination;
                        state.LastFoundPointOfInterests = null;
                    }
                }

                if (!string.IsNullOrEmpty(state.Address) && state.LastFoundPointOfInterests != null)
                {
                    // Set Destionation if one w/ matching address is found in FoundLocations
                    var destination = state.LastFoundPointOfInterests?.FirstOrDefault(x => x.City.Contains(state.Address, StringComparison.InvariantCultureIgnoreCase));
                    if (destination != null)
                    {
                        state.Destination = destination;
                        state.LastFoundPointOfInterests = null;
                    }
                }

                if (state.UserSelectIndex >= 0 && state.UserSelectIndex < state.LastFoundPointOfInterests.Count)
                {
                    // Set Destionation if one w/ matching address is found in FoundLocations
                    var destination = state.LastFoundPointOfInterests?[state.UserSelectIndex];
                    if (destination != null)
                    {
                        state.Destination = destination;
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

        public async Task<DialogTurnResult> CheckIfDestinationExists(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                // Determine if the skill needs to know of a "destination"
                if (!string.IsNullOrEmpty(state.CommonLocation) && string.IsNullOrEmpty(state.Keyword))
                {
                    var destination = state.GetCommonLocationCoordinates(state.CommonLocation);
                    if (state.Destination == null)
                    {
                        state.Destination = new PointOfInterestModel();
                    }

                    state.Destination.Name = state.CommonLocation;
                    state.Destination.Geolocation = destination;
                }

                if (state.Destination == null || (state.Destination != null && state.CommonLocation.Equals("destination") && !string.IsNullOrEmpty(state.Keyword)))
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

        public async Task<DialogTurnResult> GetRoutesToDestination(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var service = ServiceManager.InitRoutingMapsService(Services);
                var routeDirections = new RouteDirections();

                if (state.Destination == null)
                {
                    // No ActiveLocation found
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(RouteResponses.MissingActiveLocationErrorMessage, ResponseBuilder) });
                }

                routeDirections = await service.GetRouteDirectionsToDestinationAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Destination.Geolocation.Latitude, state.Destination.Geolocation.Longitude, state.RouteType);

                await GetRouteDirectionsViewCards(sc, routeDirections);

                if (routeDirections?.Routes?.ToList().Count == 1)
                {
                    return await sc.PromptAsync(Action.ConfirmPrompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(RouteResponses.PromptToStartRoute, ResponseBuilder) });
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

                    var replyMessage = sc.Context.Activity.CreateReply(RouteResponses.SendingRouteDetails);
                    await sc.Context.SendActivityAsync(replyMessage);

                    // Send event with active route data
                    var replyEvent = sc.Context.Activity.CreateReply();
                    replyEvent.Type = ActivityTypes.Event;
                    replyEvent.Name = "ActiveRoute.Directions";

                    DirectionsEventResponse eventPayload = new DirectionsEventResponse();
                    eventPayload.Destination = state.Destination;
                    eventPayload.Route = state.ActiveRoute;
                    replyEvent.Value = eventPayload;

                    await sc.Context.SendActivityAsync(replyEvent);
                }
                else
                {
                    var replyMessage = sc.Context.Activity.CreateReply(RouteResponses.AskAboutRouteLater);
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
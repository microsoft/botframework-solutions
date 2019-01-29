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
                if (state.FoundLocations == null)
                {
                    return await sc.NextAsync();
                }

                if (!string.IsNullOrEmpty(state.SearchText))
                {
                    // Set ActiveLocation if one w/ matching name is found in FoundLocations
                    var activeLocation = state.FoundLocations?.FirstOrDefault(x => x.Name.Contains(state.SearchText, StringComparison.InvariantCultureIgnoreCase));
                    if (activeLocation != null)
                    {
                        state.ActiveLocation = activeLocation;
                        state.FoundLocations = null;
                    }
                }

                if (!string.IsNullOrEmpty(state.SearchAddress) && state.FoundLocations != null)
                {
                    // Set ActiveLocation if one w/ matching address is found in FoundLocations
                    var activeLocation = state.FoundLocations?.FirstOrDefault(x => x.Address.FormattedAddress.Contains(state.SearchAddress, StringComparison.InvariantCultureIgnoreCase));
                    if (activeLocation != null)
                    {
                        state.ActiveLocation = activeLocation;
                        state.FoundLocations = null;
                    }
                }

                if (state.LastUtteredNumber != null && state.FoundLocations != null)
                {
                    // Set ActiveLocation if one w/ matching address is found in FoundLocations
                    var indexNumber = (int)state.LastUtteredNumber[0] - 1;
                    var activeLocation = state.FoundLocations?[indexNumber];
                    if (activeLocation != null)
                    {
                        state.ActiveLocation = activeLocation;
                        state.FoundLocations = null;
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
                var service = ServiceManager.InitMapsService(GetAzureMapsKey());
                var routeDirections = new RouteDirections();

                state.CheckForValidCurrentCoordinates();

                if (state.ActiveLocation == null)
                {
                    // No ActiveLocation found
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(RouteResponses.MissingActiveLocationErrorMessage, ResponseBuilder) });
                }

                if (!string.IsNullOrEmpty(state.SearchDescriptor))
                {
                    routeDirections = await service.GetRouteDirectionsAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.ActiveLocation.Point.Coordinates[0], state.ActiveLocation.Point.Coordinates[1], state.SearchDescriptor);

                    await GetRouteDirectionsViewCards(sc, routeDirections);
                }
                else
                {
                    routeDirections = await service.GetRouteDirectionsAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.ActiveLocation.Point.Coordinates[0], state.ActiveLocation.Point.Coordinates[1]);

                    await GetRouteDirectionsViewCards(sc, routeDirections);
                }

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
                    eventPayload.Destination = state.ActiveLocation;
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
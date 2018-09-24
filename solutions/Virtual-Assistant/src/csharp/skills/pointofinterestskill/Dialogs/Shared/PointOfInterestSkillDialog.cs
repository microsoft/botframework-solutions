// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using PointOfInterestSkill.Dialogs.Shared.Resources;

namespace PointOfInterestSkill
{
    public class PointOfInterestSkillDialog : ComponentDialog
    {
        // Fields
        private IServiceManager _serviceManager;
        private PointOfInterestSkillAccessors _accessors;
        private PointOfInterestSkillServices _services;
        private PointOfInterestBotResponseBuilder _responseBuilder = new PointOfInterestBotResponseBuilder();

        public PointOfInterestSkillDialog(string dialogId, PointOfInterestSkillServices services, PointOfInterestSkillAccessors accessors, IServiceManager serviceManager)
        : base(dialogId)
        {
            _services = services;
            _accessors = accessors;
            _serviceManager = serviceManager;

            AddDialog(new TextPrompt(Action.Prompt, CustomPromptValidatorAsync));
        }

        // Shared Steps

        /// <summary>
        /// Check if the state has an ActiveRoute stored and reroute if appropriate.
        /// </summary>
        public async Task<DialogTurnResult> CheckIfActiveRouteExists(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.PointOfInterestSkillState.GetAsync(sc.Context);
                if (state.ActiveRoute != null)
                {
                    await sc.EndDialogAsync(true);
                    return await sc.BeginDialogAsync(Action.FindAlongRoute);
                }

                return await sc.ContinueDialogAsync();
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(PointOfInterestBotResponses.PointOfInterestErrorMessage, _responseBuilder));
                var state = await _accessors.PointOfInterestSkillState.GetAsync(sc.Context);
                state.Clear();
                await _accessors.PointOfInterestSkillState.SetAsync(sc.Context, state);
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> CheckIfActiveLocationExists(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.PointOfInterestSkillState.GetAsync(sc.Context);
                if (state.ActiveLocation == null)
                {
                    await sc.EndDialogAsync(true);
                    return await sc.BeginDialogAsync(Action.FindPointOfInterest);
                }

                return await sc.BeginDialogAsync(Action.FindRouteToActiveLocation);
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(PointOfInterestBotResponses.PointOfInterestErrorMessage, _responseBuilder));
                var state = await _accessors.PointOfInterestSkillState.GetAsync(sc.Context);
                state.Clear();
                await _accessors.PointOfInterestSkillState.SetAsync(sc.Context, state);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Call Maps Service to run a fuzzy search from current coordinates based on entities retrieved by bot.
        /// </summary>
        public async Task<DialogTurnResult> GetPointOfInterestLocations(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                string country = "US";

                // Defensive for scenarios where locale isn't correctly set
                try
                {
                    var cultureInfo = new RegionInfo(sc.Context.Activity.Locale);
                    country = cultureInfo.TwoLetterISORegionName;
                }
                catch (Exception)
                {
                    // Default to everything if we can't restrict the country
                }

                var state = await _accessors.PointOfInterestSkillState.GetAsync(sc.Context);
                var service = _serviceManager.InitMapsService(_services.AzureMapsKey);

                if (string.IsNullOrEmpty(state.SearchText) && string.IsNullOrEmpty(state.SearchAddress))
                {
                    // No entities identified, find nearby locations
                    var locationSet = await service.GetLocationsNearby(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude);
                    await GetPointOfInterestLocationViewCards(sc, locationSet);
                }
                else if (!string.IsNullOrEmpty(state.SearchText))
                {
                    // Fuzzy search
                    var locationSet = await service.GetLocationsByFuzzyQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.SearchText, country);
                    await GetPointOfInterestLocationViewCards(sc, locationSet);
                }
                else if (!string.IsNullOrEmpty(state.SearchAddress))
                {
                    // Query search
                    var locationSet = await service.GetLocationsByFuzzyQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.SearchAddress, country);
                    await GetPointOfInterestLocationViewCards(sc, locationSet);
                }

                return await sc.EndDialogAsync(true);
            }
            catch (Exception e)
            {
                TelemetryClient tc = new TelemetryClient();
                tc.TrackException(e);

                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(PointOfInterestBotResponses.PointOfInterestErrorMessage, _responseBuilder));
                var state = await _accessors.PointOfInterestSkillState.GetAsync(sc.Context);
                state.Clear();
                await _accessors.PointOfInterestSkillState.SetAsync(sc.Context, state);
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// TODO: How to check for both text and value from activity?.
        /// </summary>
        public Task<bool> CustomPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var result = promptContext.Recognized.Value;

            return Task.FromResult(true);
        }

        /// <summary>
        /// Bot takes ActiveLocation and calls Maps API to get directions from current location.
        /// </summary>
        public async Task<DialogTurnResult> GetRoutesToActiveLocation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.PointOfInterestSkillState.GetAsync(sc.Context);
                var service = _serviceManager.InitMapsService(_services.AzureMapsKey);

                if (state.ActiveLocation == null)
                {
                    // No ActiveLocation found
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(PointOfInterestBotResponses.MissingActiveLocationErrorMessage, _responseBuilder) });
                }

                if (state.SearchDescriptor != null)
                {
                    var routeDirections = await service.GetRouteDirectionsAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.ActiveLocation.Point.Coordinates[0], state.ActiveLocation.Point.Coordinates[1], state.SearchDescriptor);

                    await GetRouteDirectionsViewCards(sc, routeDirections);
                }
                else
                {
                    var routeDirections = await service.GetRouteDirectionsAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.ActiveLocation.Point.Coordinates[0], state.ActiveLocation.Point.Coordinates[1]);

                    await GetRouteDirectionsViewCards(sc, routeDirections);
                }

                return await sc.EndDialogAsync();
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(PointOfInterestBotResponses.PointOfInterestErrorMessage, _responseBuilder));
                var state = await _accessors.PointOfInterestSkillState.GetAsync(sc.Context);
                state.Clear();
                await _accessors.PointOfInterestSkillState.SetAsync(sc.Context, state);
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> CancelActiveRoute(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessors.PointOfInterestSkillState.GetAsync(sc.Context);
                if (state.ActiveRoute != null)
                {
                    var replyMessage = sc.Context.Activity.CreateReply(PointOfInterestBotResponses.CancelActiveRoute, _responseBuilder);
                    await sc.Context.SendActivityAsync(replyMessage);
                    state.ActiveRoute = null;
                    state.ActiveLocation = null;
                }
                else
                {
                    var replyMessage = sc.Context.Activity.CreateReply(PointOfInterestBotResponses.CannotCancelActiveRoute, _responseBuilder);
                    await sc.Context.SendActivityAsync(replyMessage);
                }

                return await sc.EndDialogAsync();
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(PointOfInterestBotResponses.PointOfInterestErrorMessage, _responseBuilder));
                var state = await _accessors.PointOfInterestSkillState.GetAsync(sc.Context);
                state.Clear();
                await _accessors.PointOfInterestSkillState.SetAsync(sc.Context, state);
                return await sc.CancelAllDialogsAsync();
            }
        }

        // Helpers
        public async Task GetPointOfInterestLocationViewCards(DialogContext sc, LocationSet locationSet)
        {
            var locations = locationSet.Locations;
            var state = await _accessors.PointOfInterestSkillState.GetAsync(sc.Context);
            var cardsData = new List<LocationCardModelData>();
            var service = _serviceManager.InitMapsService(_services.AzureMapsKey);

            if (locations != null)
            {
                state.FoundLocations = locations.ToList();

                foreach (var location in locations)
                {
                    var imageUrl = service.GetLocationMapImageUrl(location);

                    LocationCardModelData locationCardModel = new LocationCardModelData()
                    {
                        ImageUrl = imageUrl,
                        LocationName = location.Name,
                        Address = location.Address.FormattedAddress,
                    };

                    cardsData.Add(locationCardModel);
                }

                if (cardsData.Count() > 1)
                {
                    if (sc.ActiveDialog.Id.Equals(Action.FindAlongRoute) && state.ActiveRoute != null)
                    {
                        var replyMessage = sc.Context.Activity.CreateAdaptiveCardGroupReply(PointOfInterestBotResponses.MultipleLocationsFoundAlongActiveRoute, "Dialogs/Shared/Resources/Cards/PointOfInterestViewCard.json", AttachmentLayoutTypes.Carousel, cardsData, _responseBuilder);
                        await sc.Context.SendActivityAsync(replyMessage);
                    }
                    else
                    {
                        var replyMessage = sc.Context.Activity.CreateAdaptiveCardGroupReply(PointOfInterestBotResponses.MultipleLocationsFound, "Dialogs/Shared/Resources/Cards/PointOfInterestViewCard.json", AttachmentLayoutTypes.Carousel, cardsData, _responseBuilder);
                        await sc.Context.SendActivityAsync(replyMessage);
                    }
                }
                else
                {
                    state.ActiveLocation = state.FoundLocations.Single();

                    if (sc.ActiveDialog.Id.Equals(Action.FindAlongRoute) && state.ActiveRoute != null)
                    {
                        var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(PointOfInterestBotResponses.SingleLocationFoundAlongActiveRoute, "Dialogs/Shared/Resources/Cards/PointOfInterestViewCard.json", cardsData.SingleOrDefault(), _responseBuilder);
                        await sc.Context.SendActivityAsync(replyMessage);
                    }
                    else
                    {
                        var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(PointOfInterestBotResponses.SingleLocationFound, "Dialogs/Shared/Resources/Cards/PointOfInterestViewCard.json", cardsData.SingleOrDefault(), _responseBuilder);
                        await sc.Context.SendActivityAsync(replyMessage);
                    }
                }
            }
            else
            {
                var replyMessage = sc.Context.Activity.CreateReply(PointOfInterestBotResponses.NoLocationsFound, _responseBuilder);
                await sc.Context.SendActivityAsync(replyMessage);
            }
        }

        public static string GetFormattedTravelTimeSpanString(TimeSpan timeSpan)
        {
            StringBuilder travelTimeSpanString = new StringBuilder();
            if (timeSpan.Hours == 1)
            {
                travelTimeSpanString.Append(timeSpan.Hours + " hour");
            }
            else if (timeSpan.Hours > 1)
            {
                travelTimeSpanString.Append(timeSpan.Hours + " hours");
            }

            if (travelTimeSpanString.Length != 0)
            {
                travelTimeSpanString.Append(" and ");
            }

            if (timeSpan.Minutes == 1)
            {
                travelTimeSpanString.Append(timeSpan.Minutes + " minute");
            }
            else if (timeSpan.Minutes > 1)
            {
                travelTimeSpanString.Append(timeSpan.Minutes + " minutes");
            }

            return travelTimeSpanString.ToString();
        }

        public static string GetFormattedTrafficDelayString(TimeSpan timeSpan)
        {
            StringBuilder trafficDelayTimeSpanString = new StringBuilder();
            if (timeSpan.Hours == 1)
            {
                trafficDelayTimeSpanString.Append(timeSpan.Hours + " hour");
            }
            else if (timeSpan.Hours > 1)
            {
                trafficDelayTimeSpanString.Append(timeSpan.Hours + " hours");
            }

            if (trafficDelayTimeSpanString.Length != 0)
            {
                trafficDelayTimeSpanString.Append(" and ");
            }

            if (timeSpan.Minutes == 1)
            {
                trafficDelayTimeSpanString.Append(timeSpan.Minutes + " minute.");
            }
            else if (timeSpan.Minutes > 1)
            {
                trafficDelayTimeSpanString.Append(timeSpan.Minutes + " minutes.");
            }

            if (trafficDelayTimeSpanString.Length != 0)
            {
                trafficDelayTimeSpanString.Insert(0, "There is a traffic delay of ");
            }
            else
            {
                trafficDelayTimeSpanString.Append("There is no delay due to traffic.");
            }

            return trafficDelayTimeSpanString.ToString();
        }

        public async Task GetRouteDirectionsViewCards(DialogContext sc, RouteDirections routeDirections)
        {
            var routes = routeDirections.Routes;
            var state = await _accessors.PointOfInterestSkillState.GetAsync(sc.Context);
            var cardsData = new List<RouteDirectionsModelCardData>();
            var routeId = 0;

            if (routes != null)
            {
                state.FoundRoutes = routes.ToList();

                foreach (var route in routes)
                {
                    TimeSpan travelTimeSpan = TimeSpan.FromSeconds(route.Summary.TravelTimeInSeconds);
                    TimeSpan trafficTimeSpan = TimeSpan.FromSeconds(route.Summary.TrafficDelayInSeconds);

                    RouteDirectionsModelCardData routeDirectionsModel = new RouteDirectionsModelCardData()
                    {
                        Location = state.ActiveLocation.Name,
                        TravelTime = GetFormattedTravelTimeSpanString(travelTimeSpan),
                        TrafficDelay = GetFormattedTrafficDelayString(trafficTimeSpan),
                        RouteId = routeId,
                    };

                    cardsData.Add(routeDirectionsModel);
                    routeId++;
                }

                if (cardsData.Count() > 1)
                {
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardGroupReply(PointOfInterestBotResponses.MultipleRoutesFound, "Dialogs/Shared/Resources/Cards/RouteDirectionsViewCard.json", AttachmentLayoutTypes.Carousel, cardsData);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    // state.ActiveRoute = routes.Single();
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(PointOfInterestBotResponses.SingleRouteFound, "Dialogs/Shared/Resources/Cards/RouteDirectionsViewCard.json", cardsData.SingleOrDefault());
                    await sc.Context.SendActivityAsync(replyMessage);
                }
            }
            else
            {
                var replyMessage = sc.Context.Activity.CreateReply(PointOfInterestBotResponses.NoLocationsFound, _responseBuilder);
                await sc.Context.SendActivityAsync(replyMessage);
            }
        }
    }
}

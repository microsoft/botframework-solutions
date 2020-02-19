// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Utilities;
using SkillServiceLibrary.Models;

namespace PointOfInterestSkill.Dialogs
{
    public class FindParkingDialog : PointOfInterestDialogBase
    {
        public FindParkingDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            RouteDialog routeDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(nameof(FindParkingDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, httpContext)
        {
            TelemetryClient = telemetryClient;

            var checkCurrentLocation = new WaterfallStep[]
            {
                CheckForCurrentCoordinatesBeforeFindParking,
                ConfirmCurrentLocation,
                ProcessCurrentLocationSelection,
                RouteToFindFindParkingDialog
            };

            var findParking = new WaterfallStep[]
            {
                GetParkingInterestPoints,
                ProcessPointOfInterestSelection,
                ProcessPointOfInterestAction,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CheckForCurrentLocation, checkCurrentLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindParking, findParking) { TelemetryClient = telemetryClient });
            AddDialog(routeDialog ?? throw new ArgumentNullException(nameof(routeDialog)));

            // Set starting dialog for component
            InitialDialogId = Actions.CheckForCurrentLocation;
        }

        /// <summary>
        /// Check for the current coordinates and if missing, prompt user.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> CheckForCurrentCoordinatesBeforeFindParking(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
            var hasCurrentCoordinates = state.CheckForValidCurrentCoordinates();
            if (hasCurrentCoordinates || !string.IsNullOrEmpty(state.Address))
            {
                return await sc.ReplaceDialogAsync(Actions.FindParking);
            }

            return await sc.PromptAsync(Actions.CurrentLocationPrompt, new PromptOptions { Prompt = ResponseManager.GetResponse(POISharedResponses.PromptForCurrentLocation) });
        }

        /// <summary>
        /// Replaces the active dialog with the FindParking waterfall dialog.
        /// </summary>
        /// <param name="sc">WaterfallStepContext.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>DialogTurnResult.</returns>
        protected async Task<DialogTurnResult> RouteToFindFindParkingDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.ReplaceDialogAsync(Actions.FindParking);
        }

        /// <summary>
        /// Look up parking points of interest, render cards, and ask user which to route to.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> GetParkingInterestPoints(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var mapsService = ServiceManager.InitMapsService(Settings, sc.Context.Activity.Locale);
                var addressMapsService = ServiceManager.InitAddressMapsService(Settings, sc.Context.Activity.Locale);

                var pointOfInterestList = new List<PointOfInterestModel>();
                var cards = new List<Card>();

                if (!string.IsNullOrEmpty(state.Address))
                {
                    // Get first POI matched with address, if there are multiple this could be expanded to confirm which address to use
                    var pointOfInterestAddressList = await addressMapsService.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address);

                    if (pointOfInterestAddressList.Any())
                    {
                        var pointOfInterest = pointOfInterestAddressList[0];

                        // TODO nearest here is not for state.CurrentCoordinates
                        pointOfInterestList = await mapsService.GetPointOfInterestListByParkingCategoryAsync(pointOfInterest.Geolocation.Latitude, pointOfInterest.Geolocation.Longitude, state.PoiType);
                        cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList, mapsService);
                    }
                    else
                    {
                        // Find parking lot near address
                        pointOfInterestList = await mapsService.GetPointOfInterestListByParkingCategoryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.PoiType);
                        cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList, mapsService);
                    }
                }
                else
                {
                    // No entities identified, find nearby parking lots
                    pointOfInterestList = await mapsService.GetPointOfInterestListByParkingCategoryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.PoiType);
                    cards = await GetPointOfInterestLocationCards(sc, pointOfInterestList, mapsService);
                }

                if (cards.Count == 0)
                {
                    var replyMessage = ResponseManager.GetResponse(POISharedResponses.NoLocationsFound);
                    await sc.Context.SendActivityAsync(replyMessage);
                    return await sc.EndDialogAsync();
                }
                else if (cards.Count == 1)
                {
                    // only to indicate it is only one result
                    return await sc.NextAsync(true);
                }
                else
                {
                    var containerCard = await GetContainerCard(sc.Context, "PointOfInterestOverviewContainer", state.CurrentCoordinates, pointOfInterestList, addressMapsService);

                    var options = GetPointOfInterestPrompt(POISharedResponses.MultipleLocationsFound, containerCard, "Container", cards);

                    return await sc.PromptAsync(Actions.SelectPointOfInterestPrompt, options);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}

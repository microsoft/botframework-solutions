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
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using PointOfInterestSkill.Dialogs.Route;
using PointOfInterestSkill.Dialogs.Shared;
using PointOfInterestSkill.Dialogs.Shared.Resources;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.ServiceClients;

namespace PointOfInterestSkill.Dialogs.FindParking
{
    public class FindParkingDialog : PointOfInterestSkillDialog
    {
        public FindParkingDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(nameof(FindParkingDialog), services, responseManager, accessor, serviceManager, telemetryClient, httpContext)
        {
            TelemetryClient = telemetryClient;

            var findParking = new WaterfallStep[]
            {
                GetParkingInterestPoints,
                ResponseToGetRoutePrompt,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.FindParking, findParking) { TelemetryClient = telemetryClient });
            AddDialog(new RouteDialog(services, responseManager, Accessor, ServiceManager, TelemetryClient, httpContext));

            // Set starting dialog for component
            InitialDialogId = Actions.FindParking;
        }

        protected async Task<DialogTurnResult> GetParkingInterestPoints(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var mapsService = ServiceManager.InitMapsService(Services, sc.Context.Activity.Locale ?? "en-us");
                var addressMapsService = ServiceManager.InitAddressMapsService(Services, sc.Context.Activity.Locale ?? "en-us");

                var pointOfInterestList = new List<PointOfInterestModel>();

                state.CheckForValidCurrentCoordinates();

                if (!string.IsNullOrEmpty(state.Address))
                {
                    // Get first POI matched with address, if there are multiple this could be expanded to confirm which address to use
                    var pointOfInterestAddressList = await addressMapsService.GetPointOfInterestListByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address);

                    if (pointOfInterestAddressList.Any())
                    {
                        var pointOfInterest = pointOfInterestAddressList[0];
                        pointOfInterestList = await mapsService.GetPointOfInterestListByParkingCategoryAsync(pointOfInterest.Geolocation.Latitude, pointOfInterest.Geolocation.Longitude);
                        await GetPointOfInterestLocationViewCards(sc, pointOfInterestList);
                    }
                    else
                    {
                        // Find parking lot near address
                        pointOfInterestList = await mapsService.GetPointOfInterestListByParkingCategoryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude);
                        await GetPointOfInterestLocationViewCards(sc, pointOfInterestList);
                    }
                }
                else
                {
                    // No entities identified, find nearby parking lots
                    pointOfInterestList = await mapsService.GetPointOfInterestListByParkingCategoryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude);
                    await GetPointOfInterestLocationViewCards(sc, pointOfInterestList);
                }

                if (pointOfInterestList?.ToList().Count == 1)
                {
                    return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions { Prompt = ResponseManager.GetResponse(POISharedResponses.PromptToGetRoute) });
                }

                state.ClearLuisResults();

                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using PointOfInterestSkill.Dialogs.Route;
using PointOfInterestSkill.Dialogs.Shared;
using PointOfInterestSkill.Dialogs.Shared.Resources;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.ServiceClients;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PointOfInterestSkill.Dialogs.FindParking
{
    public class FindParkingDialog : PointOfInterestSkillDialog
    {
        public FindParkingDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(FindParkingDialog), services, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var findParking = new WaterfallStep[]
            {
                GetParkingInterestPoints,
                ResponseToGetRoutePrompt,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.FindParking, findParking) { TelemetryClient = telemetryClient });
            AddDialog(new RouteDialog(services, Accessor, ServiceManager, TelemetryClient));

            // Set starting dialog for component
            InitialDialogId = Action.FindParking;
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
                    var pointOfInterestAddressList = await addressMapsService.GetPointOfInterestByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address);

                    if (pointOfInterestAddressList.Any())
                    {
                        var pointOfInterest = pointOfInterestAddressList[1];
                        pointOfInterestList = await mapsService.GetPointOfInterestByParkingCategoryAsync(pointOfInterest.Geolocation.Latitude, pointOfInterest.Geolocation.Longitude);
                        await GetPointOfInterestLocationViewCards(sc, pointOfInterestList);
                    }
                    else
                    {
                        // Find parking lot near address
                        pointOfInterestList = await mapsService.GetPointOfInterestByParkingCategoryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude);
                        await GetPointOfInterestLocationViewCards(sc, pointOfInterestList);
                    }
                }
                else
                {
                    // No entities identified, find nearby parking lots
                    pointOfInterestList = await mapsService.GetPointOfInterestByParkingCategoryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude);
                    await GetPointOfInterestLocationViewCards(sc, pointOfInterestList);
                }

                if (pointOfInterestList?.ToList().Count == 1)
                {
                    return await sc.PromptAsync(Action.ConfirmPrompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(POISharedResponses.PromptToGetRoute, ResponseBuilder) });
                }

                state.ClearLuisResults();

                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await HandleDialogException(sc);
                throw;
            }
        }
    }
}

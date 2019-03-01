// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using PointOfInterestSkill.Dialogs.Route;
using PointOfInterestSkill.Dialogs.Shared;
using PointOfInterestSkill.ServiceClients;

namespace PointOfInterestSkill.Dialogs.FindPointOfInterest
{
    public class FindPointOfInterestDialog : PointOfInterestSkillDialog
    {
        public FindPointOfInterestDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<PointOfInterestSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(nameof(FindPointOfInterestDialog), services, responseManager, accessor, serviceManager, telemetryClient, httpContext)
        {
            TelemetryClient = telemetryClient;

            var checkCurrentLocation = new WaterfallStep[]
            {
                CheckForCurrentCoordinates,
                ConfirmCurrentLocation,
                ProcessCurrentLocationSelection,
                RouteToFindPointOfInterestDialog
            };

            var findPointOfInterest = new WaterfallStep[]
            {
                GetPointOfInterestLocations,
                ProcessPointOfInterestSelection,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CheckForCurrentLocation, checkCurrentLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindPointOfInterest, findPointOfInterest) { TelemetryClient = telemetryClient });
            AddDialog(new RouteDialog(services, responseManager, Accessor, ServiceManager, TelemetryClient, httpContext));

            // Set starting dialog for component
            InitialDialogId = Actions.CheckForCurrentLocation;
        }

        /// <summary>
        /// Replaces the active dialog with the FindPointOfInterest waterfall dialog.
        /// </summary>
        /// <param name="sc">WaterfallStepContext.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>DialogTurnResult.</returns>
        public async Task<DialogTurnResult> RouteToFindPointOfInterestDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);

            return await sc.ReplaceDialogAsync(Actions.FindPointOfInterest);
        }
    }
}
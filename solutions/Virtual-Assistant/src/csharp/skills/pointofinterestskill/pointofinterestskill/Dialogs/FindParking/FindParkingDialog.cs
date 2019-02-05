// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Skills;
using PointOfInterestSkill.Dialogs.Route;
using PointOfInterestSkill.Dialogs.Shared;
using PointOfInterestSkill.ServiceClients;

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
    }
}

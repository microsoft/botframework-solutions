// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;

namespace PointOfInterestSkill
{
    public class RouteDialog : PointOfInterestSkillDialog
    {
        private readonly PointOfInterestSkillAccessors _accessors;
        private readonly IServiceManager _serviceManager;

        private PointOfInterestBotResponseBuilder pointOfInterestBotResponseBuilder = new PointOfInterestBotResponseBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteDialog"/> class.
        /// </summary>
        /// <param name="accessors">Steps used in dialog.</param>
        /// <param name="serviceManager">Service manager.</param>
        public RouteDialog(PointOfInterestSkillServices services, PointOfInterestSkillAccessors accessors, IServiceManager serviceManager)
            : base(nameof(RouteDialog), services, accessors, serviceManager)
        {
            _accessors = accessors;
            _serviceManager = serviceManager;

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
            };

            var findPointOfInterest = new WaterfallStep[]
            {
                GetPointOfInterestLocations,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.GetActiveRoute, checkForActiveRouteAndLocation));
            AddDialog(new WaterfallDialog(Action.FindAlongRoute, findAlongRoute));
            AddDialog(new WaterfallDialog(Action.FindRouteToActiveLocation, findRouteToActiveLocation));
            AddDialog(new WaterfallDialog(Action.FindPointOfInterest, findPointOfInterest));

            // Set starting dialog for component
            InitialDialogId = Action.GetActiveRoute;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;

namespace PointOfInterestSkill
{
    public class CancelRouteDialog : PointOfInterestSkillDialog
    {
        private readonly PointOfInterestSkillAccessors _accessors;
        private readonly IServiceManager _serviceManager;

        private PointOfInterestBotResponseBuilder pointOfInterestBotResponseBuilder = new PointOfInterestBotResponseBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelRouteDialog"/> class.
        /// </summary>
        /// <param name="accessors">Steps used in dialog.</param>
        /// <param name="serviceManager">Service manager.</param>
        public CancelRouteDialog(PointOfInterestSkillServices services, PointOfInterestSkillAccessors accessors, IServiceManager serviceManager)
            : base(nameof(CancelRouteDialog), services, accessors, serviceManager)
        {
            _accessors = accessors;
            _serviceManager = serviceManager;

            var cancelRoute = new WaterfallStep[]
            {
                CancelActiveRoute,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.CancelActiveRoute, cancelRoute));

            // Set starting dialog for component
            InitialDialogId = Action.CancelActiveRoute;
        }
    }
}

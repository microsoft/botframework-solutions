// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Builder.Dialogs;

namespace PointOfInterestSkill
{
    public class FindPointOfInterestDialog : PointOfInterestSkillDialog
    {
        private readonly PointOfInterestSkillAccessors _accessors;
        private readonly IServiceManager _serviceManager;

        private PointOfInterestBotResponseBuilder pointOfInterestBotResponseBuilder = new PointOfInterestBotResponseBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="FindPointOfInterestDialog"/> class.
        /// </summary>
        /// <param name="accessors">Steps used in dialog.</param>
        /// <param name="serviceManager">Service manager.</param>
        public FindPointOfInterestDialog(PointOfInterestSkillServices services, PointOfInterestSkillAccessors accessors, IServiceManager serviceManager)
            : base(nameof(FindPointOfInterestDialog), services, accessors, serviceManager)
        {
            _accessors = accessors;
            _serviceManager = serviceManager;

            var findPointOfInterest = new WaterfallStep[]
            {
                GetPointOfInterestLocations,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.FindPointOfInterest, findPointOfInterest));

            // Set starting dialog for component
            InitialDialogId = Action.FindPointOfInterest;
        }
    }
}

// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace PointOfInterestSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class POISharedResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string DidntUnderstandMessage = "DidntUnderstandMessage";
        public const string CancellingMessage = "CancellingMessage";
        public const string ActionEnded = "ActionEnded";
        public const string PointOfInterestErrorMessage = "PointOfInterestErrorMessage";
        public const string PromptToGetRoute = "PromptToGetRoute";
        public const string GetRouteToActiveLocationLater = "GetRouteToActiveLocationLater";
        public const string MultipleLocationsFound = "MultipleLocationsFound";
        public const string SingleLocationFound = "SingleLocationFound";
        public const string MultipleLocationsFoundAlongActiveRoute = "MultipleLocationsFoundAlongActiveRoute";
        public const string SingleLocationFoundAlongActiveRoute = "SingleLocationFoundAlongActiveRoute";
        public const string NoLocationsFound = "NoLocationsFound";
        public const string MultipleRoutesFound = "MultipleRoutesFound";
        public const string SingleRouteFound = "SingleRouteFound";
        public const string PointOfInterestSelection = "PointOfInterestSelection";
        public const string CurrentLocationMultipleSelection = "CurrentLocationMultipleSelection";
        public const string CurrentLocationSingleSelection = "CurrentLocationSingleSelection";
        public const string PointOfInterestSuggestedActionName = "PointOfInterestSuggestedActionName";
        public const string NoTrafficDelay = "NoTrafficDelay";
        public const string TrafficDelay = "TrafficDelay";
        public const string PromptForCurrentLocation = "PromptForCurrentLocation";
    }
}

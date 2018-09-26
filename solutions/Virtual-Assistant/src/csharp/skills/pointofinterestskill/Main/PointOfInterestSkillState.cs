// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace PointOfInterestSkill
{
    public class PointOfInterestSkillState : DialogState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PointOfInterestSkillState"/> class.
        /// </summary>
        public PointOfInterestSkillState()
        {
            CurrentCoordinates = null;
            ActiveLocation = null;
            APIKey = string.Empty;
            DialogName = string.Empty;
            SearchText = string.Empty;
            SearchAddress = string.Empty;
            CurrentCoordinates = new LatLng() { Latitude = 47.640568390488625, Longitude = -122.1293731033802 };
            FoundLocations = null;
            ActiveRoute = null;
            SearchDescriptor = string.Empty;
            LastUtteredNumber = null;
        }

        public UserInformation UserInfo { get; set; }

        public LatLng CurrentCoordinates { get; set; }

        public Location ActiveLocation { get; set; }

        public List<Location> FoundLocations { get; set; }

        public RouteDirections.Route ActiveRoute { get; set; }

        public List<RouteDirections.Route> FoundRoutes { get; set; }

        public string APIKey { get; set; }

        public string DialogName { get; set; }

        public string SearchText { get; set; }

        public string SearchAddress { get; set; }

        public string SearchDescriptor { get; set; }

        public IRecognizerConvert LuisResultPassedFromSkill { get; set; }

        public DialogState ConversationDialogState { get; set; }

        public double[] LastUtteredNumber { get; set; }

        public void Clear()
        {
            CurrentCoordinates = null;
            ActiveLocation = null;
            APIKey = null;
            DialogName = string.Empty;
            SearchText = string.Empty;
            SearchAddress = string.Empty;
            SearchDescriptor = string.Empty;
            FoundLocations = null;
            LastUtteredNumber = null;
        }
    }
}

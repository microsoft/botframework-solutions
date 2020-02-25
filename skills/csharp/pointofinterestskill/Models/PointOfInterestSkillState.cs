// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using SkillServiceLibrary.Models;

namespace PointOfInterestSkill.Models
{
    public class PointOfInterestSkillState
    {
        public PointOfInterestSkillState()
        {
            UserInfo = null;
            CurrentCoordinates = new LatLng { Latitude = double.NaN, Longitude = double.NaN };
            Clear();
        }

        public UserInformation UserInfo { get; set; }

        public LatLng CurrentCoordinates { get; set; }

        public PointOfInterestModel Destination { get; set; }

        public List<PointOfInterestModel> LastFoundPointOfInterests { get; set; }

        public RouteDirections.Summary ActiveRoute { get; set; }

        public List<RouteDirections.Summary> FoundRoutes { get; set; }

        public bool ShouldInterrupt { get; set; }

        public string Keyword { get; set; }

        public string Category { get; set; }

        public string Address { get; set; }

        public string RouteType { get; set; }

        public string PoiType { get; set; }

        public DestinationActionType DestinationActionType { get; set; }

        public int UserSelectIndex { get; set; }

        public void Clear()
        {
            Destination = null;
            LastFoundPointOfInterests = null;
            ActiveRoute = null;
            FoundRoutes = null;
            DestinationActionType = DestinationActionType.None;
            ClearLuisResults();
        }

        public void ClearLuisResults()
        {
            Keyword = string.Empty;
            Category = string.Empty;
            Address = string.Empty;
            RouteType = string.Empty;
            PoiType = string.Empty;
            UserSelectIndex = -1;
        }

        public bool CheckForValidCurrentCoordinates()
        {
            if (double.IsNaN(CurrentCoordinates.Latitude) || double.IsNaN(CurrentCoordinates.Longitude))
            {
                return false;

                // throw new Exception("The bot state is missing any current coordinates. Make sure your event architecture is correctly configured.");
            }

            return true;
        }
    }
}
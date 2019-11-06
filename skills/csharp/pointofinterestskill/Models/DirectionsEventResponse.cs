// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace PointOfInterestSkill.Models
{
    public class DirectionsEventResponse
    {
        public PointOfInterestModel Destination { get; set; }

        public RouteDirections.Route Route { get; set; }
    }
}
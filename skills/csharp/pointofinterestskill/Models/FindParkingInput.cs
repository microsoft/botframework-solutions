// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using SkillServiceLibrary.Models;

namespace PointOfInterestSkill.Models
{
    public class FindParkingInput : ActionBaseInput
    {
        [JsonProperty("routeType")]
        public string RouteType { get; set; }

        [JsonProperty("showRoute")]
        public bool? ShowRoute { get; set; }

        public override void DigestActionInput(PointOfInterestSkillState state)
        {
            base.DigestActionInput(state);
            if (!string.IsNullOrEmpty(RouteType))
            {
                if (RouteType == GeoSpatialServiceTypes.RouteType.Eco ||
                    RouteType == GeoSpatialServiceTypes.RouteType.Thrilling ||
                    RouteType == GeoSpatialServiceTypes.RouteType.Fastest ||
                    RouteType == GeoSpatialServiceTypes.RouteType.Shortest)
                {
                    state.RouteType = RouteType;
                }
            }

            if (ShowRoute.HasValue)
            {
                if (ShowRoute.Value)
                {
                    state.DestinationActionType = DestinationActionType.ShowDirectionsThenStartNavigation;
                }
                else
                {
                    state.DestinationActionType = DestinationActionType.StartNavigation;
                }
            }
        }
    }
}

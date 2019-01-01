// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace PointOfInterestSkill.Models
{
    public class SearchResult
    {
        [JsonProperty(PropertyName = "type")]
        public string ResultType { get; set; }

        [JsonProperty(PropertyName = "address")]
        public SearchAddress Address { get; set; }

        [JsonProperty(PropertyName = "position")]
        public LatLng Position { get; set; }

        [JsonProperty(PropertyName = "poi")]
        public PoiInfo Poi { get; set; }

        [JsonProperty(PropertyName = "viewport")]
        public Viewport Viewport { get; set; }

        public Location ToLocation()
        {
            var c = new GeocodePoint()
            {
                Coordinates = new System.Collections.Generic.List<double>
                {
                    Position.Latitude, Position.Longitude,
                },
            };

            return new Location()
            {
                Address = (Address != null) ? Address.ToBingAddress() : null,
                Point = c,
                BoundaryBox = (Viewport != null) ? new System.Collections.Generic.List<double>()
                {
                    Viewport.BtmRightPoint.Latitude, Viewport.TopLeftPoint.Longitude, Viewport.TopLeftPoint.Latitude, Viewport.BtmRightPoint.Longitude,
                }
                : null,
                GeocodePoints = new System.Collections.Generic.List<GeocodePoint>()
                {
                    c,
                },
                Name = (Poi != null && !string.IsNullOrEmpty(Poi.Name)) ? Poi.Name : Address.FreeformAddress,
                EntityType = ResultType,
            };
        }
    }
}
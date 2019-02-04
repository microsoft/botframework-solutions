// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class VenueResponse
    {
        [JsonProperty(PropertyName = "response")]
        public Response Response { get; set; }
    }
}

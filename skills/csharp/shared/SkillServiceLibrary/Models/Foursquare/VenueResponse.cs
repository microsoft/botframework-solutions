// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.Foursquare
{
    public class VenueResponse
    {
        [JsonProperty(PropertyName = "response")]
        public Response Response { get; set; }
    }
}

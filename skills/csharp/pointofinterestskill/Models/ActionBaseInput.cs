// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using SkillServiceLibrary.Models;

namespace PointOfInterestSkill.Models
{
    public class ActionBaseInput
    {
        [JsonProperty("currentLatitude")]
        public double? CurrentLatitude { get; set; }

        [JsonProperty("currentLongitude")]
        public double? CurrentLongitude { get; set; }

        [JsonProperty("keyword")]
        public string Keyword { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("poiType")]
        public string PoiType { get; set; }

        public virtual void DigestActionInput(PointOfInterestSkillState state)
        {
            state.Clear();

            if (CurrentLatitude.HasValue && CurrentLongitude.HasValue)
            {
                state.CurrentCoordinates = new SkillServiceLibrary.Models.LatLng
                {
                    Latitude = CurrentLatitude.Value,
                    Longitude = CurrentLongitude.Value,
                };
            }

            state.Keyword = Keyword;
            state.Address = Address;
            if (!string.IsNullOrEmpty(PoiType))
            {
                if (PoiType == GeoSpatialServiceTypes.PoiType.Nearest)
                {
                    state.PoiType = PoiType;
                }
            }
        }
    }
}

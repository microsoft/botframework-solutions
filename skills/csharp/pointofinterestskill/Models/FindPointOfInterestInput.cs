// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace PointOfInterestSkill.Models
{
    public class FindPointOfInterestInput : FindParkingInput
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        public override void DigestActionInput(PointOfInterestSkillState state)
        {
            base.DigestActionInput(state);
            state.Category = Category;
        }
    }
}

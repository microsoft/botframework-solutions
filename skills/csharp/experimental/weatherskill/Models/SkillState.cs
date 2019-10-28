// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace WeatherSkill.Models
{
    public class SkillState
    {
        public SkillState()
        {
            Clear();
        }

        public string Token { get; internal set; }

        public WeatherSkillLuis LuisResult { get; internal set; }

        public string Geography { get; set; }

        public double Latitude { get; set; } = double.NaN;

        public double Longitude { get; set; } = double.NaN;

        public Location GeographyLocation { get; set; }

        public void Clear()
        {
            Geography = string.Empty;
            Latitude = double.NaN;
            Longitude = double.NaN;
            GeographyLocation = null;
        }
    }
}

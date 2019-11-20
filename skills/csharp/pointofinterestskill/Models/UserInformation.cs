// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace PointOfInterestSkill.Models
{
    public class UserInformation
    {
        public string Name { get; set; }

        public TimeZoneInfo Timezone { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}
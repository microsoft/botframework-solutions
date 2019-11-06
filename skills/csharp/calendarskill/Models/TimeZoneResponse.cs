// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models
{
    public class TimeZoneResponse
    {
        public string Version { get; set; }

        public DateTime ReferenceUtcTimestamp { get; set; }

        public Timezone[] TimeZones { get; set; }

        public class Timezone
        {
            public string Id { get; set; }

            public string[] Aliases { get; set; }

            public Names Names { get; set; }

            public Referencetime ReferenceTime { get; set; }

            public Representativepoint RepresentativePoint { get; set; }

            public Timetransition[] TimeTransitions { get; set; }
        }

        public class Names
        {
            public string ISO6391LanguageCode { get; set; }

            public string Generic { get; set; }

            public string Standard { get; set; }

            public string Daylight { get; set; }
        }

        public class Referencetime
        {
            public string Tag { get; set; }

            public string StandardOffset { get; set; }

            public string DaylightSavings { get; set; }

            public DateTime WallTime { get; set; }

            public int PosixTzValidYear { get; set; }

            public string PosixTz { get; set; }

            public DateTime Sunrise { get; set; }

            public DateTime Sunset { get; set; }
        }

        public class Representativepoint
        {
            public float Latitude { get; set; }

            public float Longitude { get; set; }
        }

        public class Timetransition
        {
            public string Tag { get; set; }

            public string StandardOffset { get; set; }

            public string DaylightSavings { get; set; }

            public DateTime UtcStart { get; set; }

            public DateTime UtcEnd { get; set; }
        }
    }
}

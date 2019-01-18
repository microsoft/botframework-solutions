// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;

namespace PointOfInterestSkill.Models
{
    public class RouteDirections
    {
        public string FormatVersion { get; set; }

        public string Copyright { get; set; }

        public string Privacy { get; set; }

        public Route[] Routes { get; set; }

        public class Route
        {
            public Summary Summary { get; set; }

            public Leg[] Legs { get; set; }

            public Section[] Sections { get; set; }
        }

        public class Summary
        {
            public int LengthInMeters { get; set; }

            public int TravelTimeInSeconds { get; set; }

            public int TrafficDelayInSeconds { get; set; }

            public DateTime DepartureTime { get; set; }

            public DateTime ArrivalTime { get; set; }
        }

        public class Leg
        {
            public Summary1 Summary { get; set; }

            public Point[] Points { get; set; }
        }

        public class Summary1
        {
            public int LengthInMeters { get; set; }

            public int TravelTimeInSeconds { get; set; }

            public int TrafficDelayInSeconds { get; set; }

            public DateTime DepartureTime { get; set; }

            public DateTime ArrivalTime { get; set; }
        }

        public class Point
        {
            public float Latitude { get; set; }

            public float Longitude { get; set; }
        }

        public class Section
        {
            public int StartPointIndex { get; set; }

            public int EndPointIndex { get; set; }

            public string SectionType { get; set; }

            public string TravelMode { get; set; }
        }
    }
}
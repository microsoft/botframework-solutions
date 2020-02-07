// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkillServiceLibrary.Models
{
    public static class GeoSpatialServiceTypes
    {
        public static class PoiType
        {
            public static readonly string Nearest = "nearest";
        }

        public static class RouteType
        {
            public static readonly string Eco = "eco";
            public static readonly string Thrilling = "thrilling";
            public static readonly string Fastest = "fastest";
            public static readonly string Shortest = "shortest";
        }
    }
}

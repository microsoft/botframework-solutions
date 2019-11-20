// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WeatherSkill.Models
{
    public class Metric
    {
        public float Value { get; set; }

        public string Unit { get; set; }

        public int UnitType { get; set; }
    }
}

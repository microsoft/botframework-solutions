// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Solutions.Responses;

namespace WeatherSkill.Models
{
    public class HourDetails
    {
        public string Icon { get; set; }

        public int Temperature { get; set; }

        public string Hour { get; set; }
    }
}

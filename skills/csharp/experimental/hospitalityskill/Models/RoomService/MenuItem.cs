// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace HospitalitySkill.Models
{
    public class MenuItem : ICardData
    {
        public string Name { get; set; }

        public string[] AllNames { get; set; }

        public bool GlutenFree { get; set; }

        public bool Vegetarian { get; set; }

        public int Price { get; set; }

        public string Description { get; set; }
    }
}

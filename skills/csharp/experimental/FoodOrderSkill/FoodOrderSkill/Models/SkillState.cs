// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using FoodOrderSkill.MockBackEnd;
using System;

namespace FoodOrderSkill.Models
{
    public class SkillState
    {
        public SkillState()
        {
            OrderToPlace = null;
        }

        public string Token { get; set; }

        public FavoriteOrder OrderToPlace { get; set; }

        public TimeZoneInfo TimeZone { get; set; }

        public void Clear()
        {
        }
    }
}

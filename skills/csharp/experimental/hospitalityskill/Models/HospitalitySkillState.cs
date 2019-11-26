// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using static Luis.HospitalityLuis._Entities;

namespace HospitalitySkill.Models
{
    public class HospitalitySkillState
    {
        public HospitalityLuis LuisResult { get; set; }

        public ReservationData UpdatedReservation { get; set; }

        public double NumberEntity { get; set; }

        public List<ItemRequestClass> ItemList { get; set; }

        public List<FoodRequestClass> FoodList { get; set; }

        public void Clear()
        {
        }
    }
}

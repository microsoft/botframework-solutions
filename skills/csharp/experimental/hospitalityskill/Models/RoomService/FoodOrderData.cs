// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace HospitalitySkill.Models
{
    public class FoodOrderData : ICardData
    {
        public string Name { get; set; }

        public string SpecialRequest { get; set; }

        public int Price { get; set; }

        public int Quantity { get; set; }

        public int BillTotal { get; set; }
    }
}

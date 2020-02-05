// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace HospitalitySkill.Models
{
    public class RoomItem : ICardData
    {
        public string Item { get; set; }

        public string[] Names { get; set; }

        public int Quantity { get; set; }
    }
}

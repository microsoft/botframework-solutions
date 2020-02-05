// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace HospitalitySkill.Models
{
    public class Menu : ICardData
    {
        public string Type { get; set; }

        public string TimeAvailable { get; set; }

        public MenuItem[] Items { get; set; }
    }
}

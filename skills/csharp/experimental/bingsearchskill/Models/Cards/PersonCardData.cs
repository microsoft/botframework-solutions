// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BingSearchSkill.Responses;
using Microsoft.Bot.Solutions.Responses;

namespace BingSearchSkill.Models.Cards
{
    public class PersonCardData : ICardData
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string Title_View { get; } = CommonStrings.View;

        public string Link_View { get; set; }

        public string IconPath { get; set; }

        public string EntityTypeDisplayHint { get; set; }
    }
}

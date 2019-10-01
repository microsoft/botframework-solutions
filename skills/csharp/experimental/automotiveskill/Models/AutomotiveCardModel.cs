// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace AutomotiveSkill.Models
{
    public class AutomotiveCardModel : ICardData
    {
        public string ImageUrl { get; set; }

        public string Body { get; set; }
    }
}

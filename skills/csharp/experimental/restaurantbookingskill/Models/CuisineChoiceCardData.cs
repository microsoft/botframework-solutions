// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveCards;
using Microsoft.Bot.Solutions.Responses;

namespace RestaurantBookingSkill.Models
{
    public class CuisineChoiceCardData : ICardData
    {
        public string ImageUrl { get; set; }

        public AdaptiveImageSize ImageSize { get; set; }

        public AdaptiveHorizontalAlignment ImageAlign { get; set; }

        public string Cuisine { get; set; }
    }
}
using System;
using AdaptiveCards;
using Microsoft.Bot.Solutions.Responses;

namespace RestaurantBooking.Models
{
    public class CusineChoiceCardData : ICardData
    {
        public string ImageUrl { get; set; }

        public AdaptiveImageSize ImageSize { get; set; }

        public AdaptiveHorizontalAlignment ImageAlign { get; set; }

        public string Type { get; set; }
    }
}
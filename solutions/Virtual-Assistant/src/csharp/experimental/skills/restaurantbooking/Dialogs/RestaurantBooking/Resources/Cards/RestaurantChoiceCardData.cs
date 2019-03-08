using System;
using AdaptiveCards;
using Microsoft.Bot.Solutions.Responses;

namespace RestaurantBooking.Models
{
    public class RestaurantChoiceCardData : ICardData
    {
        public string Title { get; set; }

        public string ImageUrl { get; set; }

        public AdaptiveImageSize ImageSize { get; set; }

        public AdaptiveHorizontalAlignment ImageAlign { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public string Location { get; set; }

        public string SelectedItemData { get; set; }
    }
}
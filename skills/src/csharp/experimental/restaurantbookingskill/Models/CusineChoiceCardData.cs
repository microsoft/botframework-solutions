using AdaptiveCards;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace RestaurantBooking.Models
{
    public class CuisineChoiceCardData : ICardData
    {
        public string ImageUrl { get; set; }

        public AdaptiveImageSize ImageSize { get; set; }

        public AdaptiveHorizontalAlignment ImageAlign { get; set; }

        public string Cuisine { get; set; }
    }
}
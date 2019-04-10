using AdaptiveCards;
using Microsoft.Bot.Builder.Solutions.Shared.Responses;

namespace RestaurantBooking.Models
{
    public class TitleImageTextCardData : ICardData
    {
        public string Title { get; set; }

        public string ImageUrl { get; set; }

        public AdaptiveImageSize ImageSize { get; set; }

        public AdaptiveHorizontalAlignment ImageAlign { get; set; }

        public string CardText { get; set; }
    }
}
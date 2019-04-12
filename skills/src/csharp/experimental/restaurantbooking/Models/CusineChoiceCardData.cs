using AdaptiveCards;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace RestaurantBooking.Models
{
    public class CusineChoiceCardData : ICardData
    {
        public string ImageUrl { get; set; }

        public AdaptiveImageSize ImageSize { get; set; }

        public AdaptiveHorizontalAlignment ImageAlign { get; set; }

        public string Cusine { get; set; }
    }
}
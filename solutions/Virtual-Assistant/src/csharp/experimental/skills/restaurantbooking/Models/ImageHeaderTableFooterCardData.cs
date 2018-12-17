using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;

namespace RestaurantBooking.Models
{
    public class ImageHeaderTableFooterCardData : HeaderTableFooterCardData
    {
        public string ImageUrl { get; set; }

        public AdaptiveImageSize ImageSize { get; set; }

        public AdaptiveHorizontalAlignment ImageAlign { get; set; }
    }
}

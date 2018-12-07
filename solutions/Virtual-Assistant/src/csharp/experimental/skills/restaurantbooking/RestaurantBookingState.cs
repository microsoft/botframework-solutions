using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Graph;
using System;
using System.Collections.Generic;

namespace RestaurantBooking
{
    public class RestaurantBookingState
    {
        public RestaurantBookingState()
        {

        }

        public Skill LuisResult { get; set; }
    }
}

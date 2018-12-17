using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestaurantBooking.Dialogs.Shared.Resources;

namespace RestaurantBooking.Helpers
{
    public static class DateTimeExtensions
    {
        public static string ToSpeakString(this DateTime dateTime, bool includePrefix = false)
        {
            if (dateTime.ToShortDateString().ToLower() == DateTime.UtcNow.ToShortDateString())
            {
                return BotStrings.Today;
            }

            if (string.Equals(dateTime.ToShortDateString(), DateTime.UtcNow.AddDays(1).ToShortDateString(), StringComparison.CurrentCultureIgnoreCase))
            {
                return BotStrings.Tomorrow;
            }

            if (includePrefix && !string.IsNullOrEmpty(BotStrings.SpokenDatePrefix))
            {
                return BotStrings.SpokenDatePrefix + " " + dateTime.ToString(BotStrings.SpokenDateFormat);
            }

            return dateTime.ToString(BotStrings.SpokenDateFormat);
        }
    }
}

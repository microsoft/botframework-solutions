// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using RestaurantBookingSkill.Responses.Shared;

namespace RestaurantBookingSkill.Utilities
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

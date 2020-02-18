// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Solutions.Responses;
using RestaurantBookingSkill.Responses.Shared;

namespace RestaurantBookingSkill.Utilities
{
    public static class DateTimeExtensions
    {
        public static string ToSpeakString(this DateTime dateTime, LocaleTemplateEngineManager localeTemplateEngine, bool includePrefix = false)
        {
            if (dateTime.ToShortDateString().ToLower() == DateTime.UtcNow.ToShortDateString())
            {
                return localeTemplateEngine.GetString(BotStrings.Today);
            }

            if (string.Equals(dateTime.ToShortDateString(), DateTime.UtcNow.AddDays(1).ToShortDateString(), StringComparison.CurrentCultureIgnoreCase))
            {
                return localeTemplateEngine.GetString(BotStrings.Tomorrow);
            }

            if (includePrefix && !string.IsNullOrEmpty(localeTemplateEngine.GetString(BotStrings.SpokenDatePrefix)))
            {
                return localeTemplateEngine.GetString(BotStrings.SpokenDatePrefix) + " " + dateTime.ToString(localeTemplateEngine.GetString(BotStrings.SpokenDateFormat));
            }

            return dateTime.ToString(localeTemplateEngine.GetString(BotStrings.SpokenDateFormat));
        }
    }
}

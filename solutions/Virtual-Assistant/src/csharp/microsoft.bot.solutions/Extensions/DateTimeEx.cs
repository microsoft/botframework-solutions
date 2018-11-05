using System;
using System.Globalization;
using Microsoft.Bot.Solutions.Resources;

namespace Microsoft.Bot.Solutions.Extensions
{
    public static class DateTimeEx
    {

        /// <summary>
        /// Returns a spoken version of a date.
        /// </summary>
        public static string ToSpeechDateString(this DateTime dateTime, bool includePrefix = false)
        {
            var timeZoneOffset = dateTime.ToUniversalTime().Subtract(dateTime);
            var currentLocalTime = DateTime.UtcNow.Subtract(timeZoneOffset);
            if (dateTime.ToShortDateString().ToLower() == currentLocalTime.ToShortDateString().ToLower())
            {
                return CommonStrings.Today;
            }

            if (string.Equals(dateTime.ToShortDateString(), currentLocalTime.AddDays(1).ToShortDateString(), StringComparison.CurrentCultureIgnoreCase))
            {
                return CommonStrings.Tomorrow;
            }

            if (includePrefix && !string.IsNullOrEmpty(CommonStrings.SpokenDatePrefix))
            {
                return CommonStrings.SpokenDatePrefix + " " + dateTime.ToString(CommonStrings.SpokenDateFormat, CultureInfo.CurrentUICulture);
            }

            return dateTime.ToString(CommonStrings.SpokenDateFormat, CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// Returns a spoken version of a time
        /// </summary>
        public static string ToSpeechTimeString(this DateTime dateTime, bool includePrefix = false)
        {
            if (includePrefix)
            {
                var prefix = dateTime.Hour == 1 ? CommonStrings.SpokenTimePrefix_One : CommonStrings.SpokenTimePrefix_MoreThanOne;
                if (!string.IsNullOrEmpty(prefix))
                {
                    return prefix + " " + dateTime.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern);
                }
            }

            return dateTime.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern);
        }
    }
}
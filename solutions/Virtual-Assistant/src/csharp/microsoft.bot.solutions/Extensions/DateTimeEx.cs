using System;
using Microsoft.Bot.Solutions.Resources;

namespace Microsoft.Bot.Solutions.Extensions
{
    public static class DateTimeEx
    {
        public static string ToSpeechDateString(this DateTime dateTime, bool includePrefix = false)
        {
            if (dateTime.ToShortDateString().ToLower() == DateTime.UtcNow.ToShortDateString())
            {
                return CommonStrings.Today;
            }

            if (string.Equals(dateTime.ToShortDateString(), DateTime.UtcNow.AddDays(1).ToShortDateString(), StringComparison.CurrentCultureIgnoreCase))
            {
                return CommonStrings.Tomorrow;
            }

            if (includePrefix && !string.IsNullOrEmpty(CommonStrings.SpokenDatePrefix))
            {
                return CommonStrings.SpokenDatePrefix + " " + dateTime.ToString(CommonStrings.SpokenDateFormat);
            }

            return dateTime.ToString(CommonStrings.SpokenDateFormat);
        }

        public static string ToSpeechTimeString(this DateTime dateTime, bool includePrefix = false)
        {
            if (includePrefix && !string.IsNullOrEmpty(CommonStrings.SpokenDatePrefix))
            {
                return CommonStrings.SpokenDatePrefix + " " + dateTime.ToShortTimeString();
            }

            return dateTime.ToShortTimeString();
        }
    }
}
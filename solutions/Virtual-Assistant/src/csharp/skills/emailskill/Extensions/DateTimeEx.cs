// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill.Extensions
{
    using System;
    using Microsoft.Bot.Solutions.Resources;

    /// <summary>
    /// Date time extension to format date time.
    /// </summary>
    public static class DateTimeEx
    {
        /// <summary>
        /// Returns a string representation of this date/time. If the
        /// value is close to now, a relative description is returned.
        /// </summary>
        /// <param name="dateTime">dateTime.</param>
        /// <param name="timeZoneInfo">timeZoneInfo.</param>
        /// <returns>The formated date time string.</returns>
        public static string ToRelativeString(this DateTime dateTime, TimeZoneInfo timeZoneInfo)
        {
            // Change to local time
            var dateTimeWithTimeZone = TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZoneInfo);
            var nowWithTimeZone = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, timeZoneInfo);
            TimeSpan span = nowWithTimeZone - dateTimeWithTimeZone;

            // Normalize time span
            bool future = false;
            if (span.TotalSeconds < 0)
            {
                // In the future
                span = -span;
                future = true;
            }

            // Test for Now
            double totalSeconds = span.TotalSeconds;
            if (totalSeconds < 0.9)
            {
                return CommonStrings.Now;
            }

            string date = CommonStrings.Today;
            if (totalSeconds < (24 * 60 * 60))
            {
                // Times
                var time = dateTimeWithTimeZone.ToString(CommonStrings.DisplayTime);
                return string.Format(CommonStrings.AtTimeDetailsFormat, date, time);
            }

            // Format both date and time
            if (totalSeconds < (48 * 60 * 60))
            {
                // 1 Day
                date = future ? CommonStrings.Tomorrow : CommonStrings.Yesterday;
            }
            else if (totalSeconds < (3 * 24 * 60 * 60))
            {
                // 2 Days
                date = string.Format(CommonStrings.DaysFormat, 2);
            }
            else
            {
                // Absolute date
                if (dateTimeWithTimeZone.Year == DateTime.Now.Year)
                {
                    date = dateTimeWithTimeZone.ToString(CommonStrings.DisplayDateFormat_CurrentYear);
                }
                else
                {
                    date = dateTimeWithTimeZone.ToString(CommonStrings.DisplayDateFormat);
                }
            }

            // Add time
            return string.Format(CommonStrings.AtTimeDetailsFormat, date, dateTimeWithTimeZone.ToString(CommonStrings.DisplayTime));
        }
    }
}

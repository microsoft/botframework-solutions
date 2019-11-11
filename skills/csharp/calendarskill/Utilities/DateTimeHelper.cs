// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using CalendarSkill.Responses.Shared;

namespace CalendarSkill.Utilities
{
    public class DateTimeHelper
    {
        public static string ConvertNumberToDateTimeString(string numberString, bool convertToDate)
        {
            // convert exact number to date or time
            // if need convert to date, add "st", "rd", or "th" after the number
            // if need convert to time, add ":00" after the number
            if (int.TryParse(numberString, out var number))
            {
                if (convertToDate)
                {
                    if (number > 0 && number <= 31)
                    {
                        if (number % 10 == 1 && number != 11)
                        {
                            return string.Format(CalendarCommonStrings.OrdinalSuffixSt, numberString);
                        }
                        else if (number % 10 == 2 && number != 12)
                        {
                            return string.Format(CalendarCommonStrings.OrdinalSuffixNd, numberString);
                        }
                        else if (number % 10 == 3 && number != 13)
                        {
                            return string.Format(CalendarCommonStrings.OrdinalSuffixRd, numberString);
                        }
                        else
                        {
                            return string.Format(CalendarCommonStrings.OrdinalSuffixTh, numberString);
                        }
                    }
                }
                else
                {
                    if (number >= 0 && number <= 24)
                    {
                        return numberString + ":00";
                    }
                }
            }

            return numberString;
        }

        // StartTime.Count could be 1/2, while endTime.Count could be 0/1/2.
        public static DateTime ChooseStartTime(List<DateTime> startTimes, List<DateTime> endTimes, DateTime startTimeRestriction, DateTime endTimeRestriction, DateTime userNow)
        {
            // Only one startTime, return directly.
            if (startTimes.Count == 1)
            {
                return startTimes[0];
            }

            // Only one endTime, and startTimes[1] later than endTime. For example: start-11am/11pm, end-2pm, return the first one, while start-2am/2pm, end-3pm, return the second.
            if (endTimes.Count == 1)
            {
                return startTimes[1] > endTimes[0] ? startTimes[0] : startTimes[1];
            }

            // StartTimes[0] has passed. For example: "book a meeting from 6 to 7", and it's 10am now, we will use 6/7pm.
            if (startTimes[0] < userNow)
            {
                return startTimes[1];
            }

            // check which is valid by time restriction.
            if (IsInRange(startTimes[0], startTimeRestriction, endTimeRestriction))
            {
                return startTimes[0];
            }
            else if (IsInRange(startTimes[1], startTimeRestriction, endTimeRestriction))
            {
                return startTimes[1];
            }

            // default choose the first one.
            return startTimes[0];
        }

        public static bool IsInRange(DateTime time, DateTime startTime, DateTime endTime)
        {
            return startTime <= time && time <= endTime;
        }
    }
}

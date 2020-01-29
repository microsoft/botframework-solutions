// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using CalendarSkill.Responses.Shared;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using static CalendarSkill.Models.EventModel;

namespace CalendarSkill.Utilities
{
    public class DisplayHelper
    {
        public static string ToDisplayParticipantsStringSummary(List<Attendee> participants, int maxShowCount)
        {
            // return the multiple names with "Alice and 2 other people"
            if (participants == null || participants.Count() == 0)
            {
                return CalendarCommonStrings.NoAttendees;
            }

            var participantString = participants.GetRange(0, Math.Min(maxShowCount, participants.Count)).ToSpeechString(CommonStrings.And, li => li.DisplayName ?? li.Address);
            if (participants.Count > maxShowCount)
            {
                participantString = string.Format(CalendarCommonStrings.AttendeesSummary, participantString, participants.Count - maxShowCount);
            }

            return participantString;
        }

        public static string ToDisplayMeetingDuration(TimeSpan timeSpan)
        {
            if (timeSpan == null)
            {
                return string.Empty;
            }

            if (timeSpan.TotalHours < 1)
            {
                return string.Format(CalendarCommonStrings.ShortDisplayDurationMinute, timeSpan.Minutes);
            }
            else
            {
                if (timeSpan.Minutes == 0)
                {
                    return string.Format(CalendarCommonStrings.ShortDisplayDurationHour, timeSpan.Hours);
                }
                else
                {
                    var result = string.Format(CalendarCommonStrings.ShortDisplayDurationHour, timeSpan.Hours);
                    result += string.Format(CalendarCommonStrings.ShortDisplayDurationMinute, timeSpan.Minutes);
                    return result;
                }
            }
        }

        public static string ToDisplayDate(DateTime dateTime, TimeZoneInfo userTimezone)
        {
            // today/tomorrow/on date
            var userToday = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, userTimezone);
            var userDateTime = dateTime;
            if (userToday.Date.Equals(userDateTime.Date))
            {
                return CalendarCommonStrings.TodayLower;
            }
            else if (userToday.AddDays(1).Date.Equals(userDateTime.Date))
            {
                return CalendarCommonStrings.TomorrowLower;
            }
            else
            {
                return string.Format(CalendarCommonStrings.ShowEventDateCondition, userDateTime.ToShortDateString());
            }
        }
    }
}

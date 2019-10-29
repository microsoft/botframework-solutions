using System;
using System.Collections.Generic;
using System.Linq;
using CalendarSkill.Responses.Shared;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
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

            string result = null;

            if (timeSpan.Days > 0)
            {
                result += string.Format(CalendarCommonStrings.ShortDisplayDurationDay, timeSpan.Days);
            }

            if (timeSpan.Hours > 0)
            {
                result += string.Format(CalendarCommonStrings.ShortDisplayDurationHour, timeSpan.Hours);
            }

            if (timeSpan.Minutes > 0)
            {
                result += string.Format(CalendarCommonStrings.ShortDisplayDurationMinute, timeSpan.Minutes);
            }

            return result;
        }
    }
}

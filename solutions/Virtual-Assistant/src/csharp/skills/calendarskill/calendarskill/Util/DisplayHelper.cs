using System;
using System.Collections.Generic;
using System.Linq;
using CalendarSkill.Dialogs.Shared.Resources.Strings;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
using static CalendarSkill.Models.EventModel;

namespace CalendarSkill.Util
{
    public class DisplayHelper
    {
        public static string ToDisplayParticipantsStringSummary(List<Attendee> participants, int maxShowCount)
        {
            // return the multiple names with "Alice and 2 more"
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
                    string result = string.Format(CalendarCommonStrings.ShortDisplayDurationHour, timeSpan.Hours);
                    result += string.Format(CalendarCommonStrings.ShortDisplayDurationMinute, timeSpan.Minutes);
                    return result;
                }
            }
        }
    }
}

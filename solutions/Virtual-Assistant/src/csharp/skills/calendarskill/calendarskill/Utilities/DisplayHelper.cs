﻿using System.Collections.Generic;
using System.Linq;
using CalendarSkill.Responses.Shared;
using Microsoft.Bot.Builder.Solutions.Resources;
using static CalendarSkill.Models.EventModel;

namespace CalendarSkill.Utilities
{
    public class DisplayHelper
    {
        public static string ToDisplayParticipantsStringSummary(List<Attendee> participants)
        {
            // return the multiple names with "Alice and 2 more"
            if (participants == null || participants.Count() == 0)
            {
                return CalendarCommonStrings.NoAttendees;
            }

            var participantString = string.IsNullOrEmpty(participants[0].DisplayName) ? participants[0].Address : participants[0].DisplayName;
            if (participants.Count > 1)
            {
                participantString = string.Format(CalendarCommonStrings.AttendeesSummary, participantString, participants.Count - 1);
            }

            return participantString;
        }

        public static string ToDisplayParticipantsStringSummaryInCard(List<Attendee> participants)
        {
            // return the multiple names with "Alice + 2 more"
            if (participants == null || participants.Count() == 0)
            {
                return CalendarCommonStrings.NoAttendees;
            }

            var participantString = string.IsNullOrEmpty(participants[0].DisplayName) ? participants[0].Address : participants[0].DisplayName;
            if (participants.Count > 1)
            {
                participantString += string.Format(CommonStrings.RecipientsSummary, participants.Count - 1);
            }

            return participantString;
        }
    }
}

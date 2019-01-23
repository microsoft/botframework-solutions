using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Solutions.Resources;
using static CalendarSkill.Models.EventModel;

namespace CalendarSkill.Util
{
    public class DisplayHelper
    {
        public static string ToDisplayParticipantsStringSummary(List<Attendee> participants)
        {
            if (participants == null || participants.Count() == 0)
            {
                throw new Exception("No recipient!");
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

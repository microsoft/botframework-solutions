// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Services;
using Luis;
using Microsoft.Bot.Schema;

namespace ITSMSkill.Models
{
    public class SkillState
    {
        public SkillState()
        {
            Clear();
        }

        public TokenResponse Token { get; set; }

        public string TicketDescription { get; set; }

        public UrgencyLevel UrgencyLevel { get; set; }

        public void DigestLuisResult(ITSMLuis luis)
        {
            Clear();
            if (luis.Entities.TicketDescription != null)
            {
                TicketDescription = string.Join(' ', luis.Entities.TicketDescription);
            }
        }

        public void Clear()
        {
            Token = null;
            TicketDescription = null;
            UrgencyLevel = UrgencyLevel.None;
        }
    }
}

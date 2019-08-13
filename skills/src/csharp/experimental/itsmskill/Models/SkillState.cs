// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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

        public string Id { get; set; }

        public string TicketDescription { get; set; }

        public string CloseReason { get; set; }

        public UrgencyLevel UrgencyLevel { get; set; }

        public AttributeType AttributeType { get; set; }

        public void DigestLuisResult(ITSMLuis luis)
        {
            Clear();

            if (luis.Entities.TicketDescription != null)
            {
                TicketDescription = string.Join(' ', luis.Entities.TicketDescription);
            }

            if (luis.Entities.CloseReason != null)
            {
                CloseReason = string.Join(' ', luis.Entities.CloseReason);
            }

            // TODO only the first one is considered now
            if (luis.Entities.UrgencyLevel != null)
            {
                UrgencyLevel = Enum.Parse<UrgencyLevel>(luis.Entities.UrgencyLevel[0][0], true);
            }

            if (luis.Entities.AttributeType != null)
            {
                AttributeType = Enum.Parse<AttributeType>(luis.Entities.AttributeType[0][0], true);
            }
        }

        public void Clear()
        {
            Token = null;
            Id = null;
            TicketDescription = null;
            CloseReason = null;
            UrgencyLevel = UrgencyLevel.None;
            AttributeType = AttributeType.None;
        }
    }
}

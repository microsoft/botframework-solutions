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
            ClearLuisResult();
        }

        // always call GetAuthToken before using
        public TokenResponse Token { get; set; }

        // handle manually
        public int PageIndex { get; set; }

        // used when from ShowKnowledge to CreateTicket
        public bool SkipDisplayExisting { get; set; }

        public string Id { get; set; }

        public string TicketDescription { get; set; }

        public string CloseReason { get; set; }

        public UrgencyLevel UrgencyLevel { get; set; }

        public AttributeType AttributeType { get; set; }

        public TicketState TicketState { get; set; }

        // from OnInterruptDialogAsync
        public GeneralLuis.Intent GeneralIntent { get; set; }

        public void DigestLuisResult(ITSMLuis luis, ITSMLuis.Intent topIntent)
        {
            ClearLuisResult();

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

            if (luis.Entities.TicketState != null)
            {
                TicketState = Enum.Parse<TicketState>(luis.Entities.TicketState[0][0], true);
            }

            // TODO some special digestions
            if (topIntent == ITSMLuis.Intent.TicketUpdate)
            {
                // clear AttributeType if already set
                if (AttributeType == AttributeType.Description && !string.IsNullOrEmpty(TicketDescription))
                {
                    AttributeType = AttributeType.None;
                }
                else if (AttributeType == AttributeType.Urgency && UrgencyLevel != UrgencyLevel.None)
                {
                    AttributeType = AttributeType.None;
                }
            }
            else if (topIntent == ITSMLuis.Intent.TicketCreate)
            {
                SkipDisplayExisting = false;
            }
            else if (topIntent == ITSMLuis.Intent.TicketShow)
            {
                AttributeType = AttributeType.None;
            }
        }

        public void ClearLuisResult()
        {
            Id = null;
            TicketDescription = null;
            CloseReason = null;
            UrgencyLevel = UrgencyLevel.None;
            AttributeType = AttributeType.None;
            TicketState = TicketState.None;
        }
    }
}

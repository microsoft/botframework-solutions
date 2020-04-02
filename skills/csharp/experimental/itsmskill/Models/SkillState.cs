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
            ServiceCache = new ServiceCache();
            ClearLuisResult();
        }

        // used by Service to cache internal states
        public ServiceCache ServiceCache { get; set; }

        // handle manually
        public int PageIndex { get; set; }

        // used when from ShowKnowledge to CreateTicket
        public bool DisplayExisting { get; set; }

        public Ticket TicketTarget { get; set; }

        public ITSMLuis.Intent InterruptedIntent { get; set; }

        public string Id { get; set; }

        // Ticket search text (query) is saved here
        public string TicketTitle { get; set; }

        public string TicketDescription { get; set; }

        public string CloseReason { get; set; }

        public UrgencyLevel UrgencyLevel { get; set; }

        public AttributeType AttributeType { get; set; }

        public TicketState TicketState { get; set; }

        // INC[0-9]{7}
        public string TicketNumber { get; set; }

        public TokenResponse AccessTokenResponse { get; set; }

        // from OnInterruptDialogAsync
        public GeneralLuis.Intent GeneralIntent { get; set; }

        public void DigestLuisResult(ITSMLuis luis, ITSMLuis.Intent topIntent)
        {
            ClearLuisResult();

            if (luis.Entities.TicketTitle != null)
            {
                TicketTitle = string.Join(' ', luis.Entities.TicketTitle);
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

            if (luis.Entities.TicketNumber != null)
            {
                TicketNumber = luis.Entities.TicketNumber[0].ToUpper();
            }

            // TODO some special digestions
            if (topIntent == ITSMLuis.Intent.TicketUpdate)
            {
                // clear AttributeType if already set
                if (AttributeType == AttributeType.Title && !string.IsNullOrEmpty(TicketTitle))
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
                DisplayExisting = true;
            }
            else if (topIntent == ITSMLuis.Intent.TicketShow)
            {
                AttributeType = AttributeType.None;
            }
        }

        public void ClearLuisResult()
        {
            Id = null;
            TicketTitle = null;
            TicketDescription = null;
            CloseReason = null;
            UrgencyLevel = UrgencyLevel.None;
            AttributeType = AttributeType.None;
            TicketState = TicketState.None;
            TicketNumber = null;
        }
    }
}

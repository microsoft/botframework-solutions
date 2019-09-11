// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Responses.Shared;
using ITSMSkill.Utilities;

namespace ITSMSkill.Models
{
    // TODO same as ServiceNow's ticket state
    public enum TicketState
    {
        None,
        [EnumLocalizedDescription("TicketStateNew", typeof(SharedStrings))]
        New,
        [EnumLocalizedDescription("TicketStateInProgress", typeof(SharedStrings))]
        InProgress,
        [EnumLocalizedDescription("TicketStateOnHold", typeof(SharedStrings))]
        OnHold,
        [EnumLocalizedDescription("TicketStateResolved", typeof(SharedStrings))]
        Resolved,
        [EnumLocalizedDescription("TicketStateClosed", typeof(SharedStrings))]
        Closed,
        [EnumLocalizedDescription("TicketStateCanceled", typeof(SharedStrings))]
        Canceled
    }
}

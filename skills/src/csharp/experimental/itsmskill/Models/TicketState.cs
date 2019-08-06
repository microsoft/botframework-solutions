using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models
{
    // TODO same as ServiceNow's ticket state
    public enum TicketState
    {
        None,
        New,
        InProgress,
        OnHold,
        Resolved,
        Closed,
        Canceled
    }
}

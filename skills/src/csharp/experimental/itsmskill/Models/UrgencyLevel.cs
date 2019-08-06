using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models
{
    // TODO same as ServiceNow's Urgency. However it is mapped to Priority internally
    public enum UrgencyLevel
    {
        None,
        Low,
        Medium,
        High
    }
}

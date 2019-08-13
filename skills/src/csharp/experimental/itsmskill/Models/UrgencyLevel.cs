using ITSMSkill.Responses.Shared;
using ITSMSkill.Utilities;

namespace ITSMSkill.Models
{
    // TODO same as ServiceNow's Urgency. However it is mapped to Priority internally
    public enum UrgencyLevel
    {
        None,
        [EnumLocalizedDescription("UrgencyLow", typeof(SharedStrings))]
        Low,
        [EnumLocalizedDescription("UrgencyMedium", typeof(SharedStrings))]
        Medium,
        [EnumLocalizedDescription("UrgencyHigh", typeof(SharedStrings))]
        High
    }
}

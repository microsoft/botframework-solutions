using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace ITSMSkill.Extensions.Teams
{
    public enum TeamsFlowType
    {
        /// <summary>
        /// Task Module will display create subscription
        /// </summary>
        [EnumMember(Value = "createsubscription_form")]
        CreateSubscription_Form,

        /// <summary>
        /// Task Module will display create subscription
        /// </summary>
        [EnumMember(Value = "createticket_form")]
        CreateTicket_Form,

        /// <summary>
        /// Task Module will display create subscription
        /// </summary>
        [EnumMember(Value = "updateticket_form")]
        UpdateTicket_Form,
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models.ServiceNow
{
    public class MultiTicketsResponse
    {
        public List<TicketResponse> result { get; set; }
    }
}

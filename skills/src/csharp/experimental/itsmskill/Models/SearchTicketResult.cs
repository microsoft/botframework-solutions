using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models
{
    public class SearchTicketResult : ResultBase
    {
        public Ticket[] Tickets { get; set; }
    }
}

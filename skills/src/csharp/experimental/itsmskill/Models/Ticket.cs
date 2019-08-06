﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models
{
    public class Ticket
    {
        public string Id { get; set; }

        public string Description { get; set; }

        public UrgencyLevel Urgency { get; set; }

        public TicketState State { get; set; }

        public DateTime OpenedTime { get; set; }

        public string ResolvedReason { get; set; }
    }
}

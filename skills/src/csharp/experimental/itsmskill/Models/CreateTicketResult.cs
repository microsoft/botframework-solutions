﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models
{
    public class CreateTicketResult : ResultBase
    {
        public Ticket Ticket { get; set; }
    }
}

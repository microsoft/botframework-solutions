using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhoSkill.Models
{
    public class Attendee
    {
        public ResponseStatus Status { get; set; }

        public string Type { get; set; }

        public EmailAddress EmailAddress { get; set; }
    }
}

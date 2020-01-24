using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhoSkill.Models
{
    public class EventSearchResult
    {
        public string ODataEtag { get; set; }

        public string Id { get; set; }

        public IEnumerable<Attendee> Attendees { get; set; }

        public EventOrganizer Organizer { get; set; }
    }
}

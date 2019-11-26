using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models
{
    public class AvailabilityResult
    {
        public List<string> AvailabilityViewList { get; set; } = new List<string>();

        public List<EventModel> MySchedule { get; set; } = new List<EventModel>();
    }
}

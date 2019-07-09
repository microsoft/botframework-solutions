using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models.DialogModel
{
    public class ShowMeetingsDialogState : CalendarDialogStateBase
    {
        public ShowMeetingsDialogState(CalendarDialogStateBase state = null)
            : base(state)
        {
            ReadOutEvents = new List<EventModel>();
        }

        public string StartDateString { get; set; }

        public string AskParameterContent { get; set; }

        public int TotalConflictCount { get; set; }

        public string FilterMeetingKeyWord { get; set; }

        public List<EventModel> ReadOutEvents { get; set; }
    }
}

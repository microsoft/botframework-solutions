using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models.DialogModel
{
    public class ChangeEventStatusDialogState : CalendarDialogStateBase
    {
        public ChangeEventStatusDialogState(CalendarDialogStateBase state = null)
            : base(state)
        {
            RecurrencePattern = null;
            NewEventStatus = EventStatus.None;
        }

        public string RecurrencePattern { get; set; }

        public EventStatus NewEventStatus { get; set; }
    }
}

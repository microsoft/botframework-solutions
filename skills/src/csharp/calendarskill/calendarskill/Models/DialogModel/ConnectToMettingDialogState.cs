using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models.DialogModel
{
    public class ConnectToMettingDialogState : CalendarDialogStateBase
    {
        public ConnectToMettingDialogState(CalendarDialogStateBase state = null)
               : base(state)
        {
            ConfirmedMeeting = new List<EventModel>();
        }

        public List<EventModel> ConfirmedMeeting { get; set; }
    }
}

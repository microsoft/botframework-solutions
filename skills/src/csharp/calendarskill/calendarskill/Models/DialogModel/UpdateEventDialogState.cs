using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models.DialogModel
{
    public class UpdateEventDialogState : CalendarDialogStateBase
    {
        public UpdateEventDialogState(CalendarDialogStateBase state = null)
            : base(state)
        {
            OriginalStartDate = new List<DateTime>();
            OriginalStartTime = new List<DateTime>();
            OriginalEndDate = new List<DateTime>();
            OriginalEndTime = new List<DateTime>();
            NewStartDate = new List<DateTime>();
            NewStartTime = new List<DateTime>();
            NewEndDate = new List<DateTime>();
            NewEndTime = new List<DateTime>();
            NewStartDateTime = null;
            RecurrencePattern = null;
        }

        // user time zone
        public List<DateTime> OriginalStartDate { get; set; }

        // user time zone
        public List<DateTime> OriginalStartTime { get; set; }

        // user time zone
        public List<DateTime> OriginalEndDate { get; set; }

        // user time zone
        public List<DateTime> OriginalEndTime { get; set; }

        // user time zone
        public List<DateTime> NewStartDate { get; set; }

        // user time zone
        public List<DateTime> NewStartTime { get; set; }

        // user time zone
        public List<DateTime> NewEndDate { get; set; }

        // user time zone
        public List<DateTime> NewEndTime { get; set; }

        // UTC
        public DateTime? NewStartDateTime { get; set; }

        public int MoveTimeSpan { get; set; }

        public string RecurrencePattern { get; set; }
    }
}

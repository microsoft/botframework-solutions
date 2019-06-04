using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models.DialogModel
{
    public class ShowMeetingsDialogState : CalendarDialogStateBase
    {
        public ShowMeetingsDialogState()
            : base()
        {
            ReadOutEvents = new List<EventModel>();
        }

        public ShowMeetingsDialogState(CalendarDialogStateBase state)
        {
            Title = state.Title;
            StartDate = state.StartDate;
            StartTime = state.StartTime;
            StartDateTime = state.StartDateTime;
            EndDate = state.EndDate;
            EndTime = state.EndTime;
            EndDateTime = state.EndDateTime;
            OrderReference = state.OrderReference;
            Attendees = state.Attendees;
            Events = state.Events;
            ShowEventIndex = state.ShowEventIndex;
            IsActionFromSummary = state.IsActionFromSummary;
            FilterMeetingKeyWord = state.FilterMeetingKeyWord;
        }

        public string StartDateString { get; set; }

        public List<EventModel> ReadOutEvents { get; set; }

        public string AskParameterContent { get; set; }

        public int TotalConflictCount { get; set; }
    }
}

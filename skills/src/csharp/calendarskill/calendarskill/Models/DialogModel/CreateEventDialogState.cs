using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CalendarSkill.Models.CreateEventStateModel;

namespace CalendarSkill.Models.DialogModel
{
    public class CreateEventDialogState : CalendarDialogStateBase
    {
        public CreateEventDialogState(CalendarDialogStateBase state = null)
            : base(state)
        {
            Content = string.Empty;
            Location = string.Empty;
            Duration = 0;
            CreateHasDetail = false;
            RecreateState = null;
            FindContactInfor = new FindContactInformation();
        }

        public string Content { get; set; }

        public string Location { get; set; }

        public int Duration { get; set; }

        public bool CreateHasDetail { get; set; }

        public RecreateEventState? RecreateState { get; set; }

        public FindContactInformation FindContactInfor { get; set; }

        public void ClearTimes()
        {
            StartDate = new List<DateTime>();
            StartTime = new List<DateTime>();
            StartDateTime = null;
            EndDate = new List<DateTime>();
            EndTime = new List<DateTime>();
            EndDateTime = null;
            CreateHasDetail = true;
            RecreateState = RecreateEventState.Time;
        }

        public void ClearTimesExceptStartTime()
        {
            EndDate = new List<DateTime>();
            EndTime = new List<DateTime>();
            EndDateTime = null;
            Duration = 0;
            CreateHasDetail = true;
            RecreateState = RecreateEventState.Duration;
        }

        public void ClearLocation()
        {
            Location = null;
            CreateHasDetail = true;
            RecreateState = RecreateEventState.Location;
        }

        public void ClearParticipants()
        {
            FindContactInfor.Contacts = new List<EventModel.Attendee>();
            FindContactInfor.ContactsNameList = new List<string>();
            FindContactInfor.CurrentContactName = string.Empty;
            FindContactInfor.ConfirmContactsNameIndex = 0;
            CreateHasDetail = true;
            RecreateState = RecreateEventState.Participants;
        }

        public void ClearSubject()
        {
            Title = null;
            CreateHasDetail = true;
            RecreateState = RecreateEventState.Subject;
        }

        public void ClearContent()
        {
            Content = null;
            CreateHasDetail = true;
            RecreateState = RecreateEventState.Content;
        }

    }
}

using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models
{
    public class CalendarDialogStateBase
    {
        public CalendarDialogStateBase()
        {
            Title = null;
            StartDate = new List<DateTime>();
            StartTime = new List<DateTime>();
            StartDateTime = null;
            EndDate = new List<DateTime>();
            EndTime = new List<DateTime>();
            EndDateTime = null;
            OrderReference = null;
            Attendees = new List<EventModel.Attendee>();
            Events = new List<EventModel>();
            ShowEventIndex = 0;
            IsActionFromSummary = false;
            FilterMeetingKeyWord = null;
            SummaryEvents = null;
        }

        public string Title { get; set; }

        // user time zone
        public List<DateTime> StartDate { get; set; }

        // user time zone
        public List<DateTime> StartTime { get; set; }

        // UTC
        public DateTime? StartDateTime { get; set; }

        // user time zone
        public List<DateTime> EndDate { get; set; }

        // user time zone
        public List<DateTime> EndTime { get; set; }

        // UTC
        public DateTime? EndDateTime { get; set; }

        // the order reference, such as 'next'
        public string OrderReference { get; set; }

        public List<EventModel.Attendee> Attendees { get; set; }

        public List<EventModel> Events { get; set; }

        public int ShowEventIndex { get; set; }

        public bool IsActionFromSummary { get; set; }

        public string FilterMeetingKeyWord { get; set; }

        public List<EventModel> SummaryEvents { get; set; }

        public void Clear()
        {
            Title = null;
            StartDate = new List<DateTime>();
            StartTime = new List<DateTime>();
            StartDateTime = null;
            EndDate = new List<DateTime>();
            EndTime = new List<DateTime>();
            EndDateTime = null;
            OrderReference = null;
            Attendees = new List<EventModel.Attendee>();
            Events = new List<EventModel>();
            ShowEventIndex = 0;
            IsActionFromSummary = false;
            FilterMeetingKeyWord = null;
            SummaryEvents = null;
        }

        //public void ClearChangeStautsInfo()
        //{
        //    // only clear change status flow related info if begin the flow from summary
        //    NewEventStatus = EventStatus.None;
        //    Events = new List<EventModel>();
        //    IsActionFromSummary = false;
        //}

        //public void ClearUpdateEventInfo()
        //{
        //    // only clear update meeting flow related info if begin the flow from summary
        //    OriginalStartDate = new List<DateTime>();
        //    OriginalStartTime = new List<DateTime>();
        //    OriginalEndDate = new List<DateTime>();
        //    OriginalEndTime = new List<DateTime>();
        //    NewStartDate = new List<DateTime>();
        //    NewStartTime = new List<DateTime>();
        //    NewEndDate = new List<DateTime>();
        //    NewEndTime = new List<DateTime>();
        //    Events = new List<EventModel>();
        //    IsActionFromSummary = false;
        //}

        //public void ClearTimes()
        //{
        //    StartDate = new List<DateTime>();
        //    StartDateString = null;
        //    StartTime = new List<DateTime>();
        //    StartTimeString = null;
        //    StartDateTime = null;
        //    EndDate = new List<DateTime>();
        //    EndTime = new List<DateTime>();
        //    EndDateTime = null;
        //    OriginalStartDate = new List<DateTime>();
        //    OriginalStartTime = new List<DateTime>();
        //    OriginalEndDate = new List<DateTime>();
        //    OriginalEndTime = new List<DateTime>();
        //    NewStartDateTime = null;
        //    MoveTimeSpan = 0;
        //    CreateHasDetail = true;
        //    RecreateState = RecreateEventState.Time;
        //}

        //public void ClearTimesExceptStartTime()
        //{
        //    EndDate = new List<DateTime>();
        //    EndTime = new List<DateTime>();
        //    EndDateTime = null;
        //    OriginalStartDate = new List<DateTime>();
        //    OriginalStartTime = new List<DateTime>();
        //    OriginalEndDate = new List<DateTime>();
        //    OriginalEndTime = new List<DateTime>();
        //    NewStartDateTime = null;
        //    Duration = 0;
        //    MoveTimeSpan = 0;
        //    CreateHasDetail = true;
        //    RecreateState = RecreateEventState.Duration;
        //}

        //public void ClearLocation()
        //{
        //    Location = null;
        //    CreateHasDetail = true;
        //    RecreateState = RecreateEventState.Location;
        //}

        //public void ClearParticipants()
        //{
        //    Attendees = new List<EventModel.Attendee>();
        //    AttendeesNameList = new List<string>();
        //    CurrentAttendeeName = string.Empty;
        //    ConfirmAttendeesNameIndex = 0;
        //    CreateHasDetail = true;
        //    RecreateState = RecreateEventState.Participants;
        //}

        //public void ClearSubject()
        //{
        //    Title = null;
        //    CreateHasDetail = true;
        //    RecreateState = RecreateEventState.Subject;
        //}

        //public void ClearContent()
        //{
        //    Content = null;
        //    CreateHasDetail = true;
        //    RecreateState = RecreateEventState.Content;
        //}

        //public class CustomizedPerson
        //{
        //    public CustomizedPerson()
        //    {
        //    }

        //    public CustomizedPerson(PersonModel person)
        //    {
        //        this.Emails = new List<ScoredEmailAddress>();
        //        person.Emails.ToList().ForEach(e => this.Emails.Add(new ScoredEmailAddress() { Address = e }));
        //        this.DisplayName = person.DisplayName;
        //        this.UserPrincipalName = person.UserPrincipalName;
        //    }

        //    public List<ScoredEmailAddress> Emails { get; set; }

        //    public string DisplayName { get; set; }

        //    public string UserPrincipalName { get; set; }
        //}
    }
}

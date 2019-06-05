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
            InitData();
        }

        public CalendarDialogStateBase(CalendarDialogStateBase state)
        {
            if (state != null)
            {
                Title = state.Title;
                StartDate = state.StartDate;
                StartTime = state.StartTime;
                StartDateTime = state.StartDateTime;
                EndDate = state.EndDate;
                EndTime = state.EndTime;
                EndDateTime = state.EndDateTime;
                OrderReference = state.OrderReference;
                Events = state.Events;
                ShowEventIndex = state.ShowEventIndex;
                FilterMeetingKeyWord = state.FilterMeetingKeyWord;
            }
            else
            {
                InitData();
            }
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

        public List<EventModel> Events { get; set; }

        public int ShowEventIndex { get; set; }

        public string FilterMeetingKeyWord { get; set; }

        public List<EventModel> SummaryEvents { get; set; }

        public void Clear()
        {
            InitData();
        }

        private void InitData()
        {
            Title = null;
            StartDate = new List<DateTime>();
            StartTime = new List<DateTime>();
            StartDateTime = null;
            EndDate = new List<DateTime>();
            EndTime = new List<DateTime>();
            EndDateTime = null;
            OrderReference = null;
            Events = new List<EventModel>();
            ShowEventIndex = 0;
            FilterMeetingKeyWord = null;
            SummaryEvents = null;
        }

        public class FindContactInformation
        {
            public FindContactInformation()
            {
                CurrentContactName = string.Empty;
                ContactsNameList = new List<string>();
                Contacts = new List<EventModel.Attendee>();
                ConfirmContactsNameIndex = 0;
                ShowContactsIndex = 0;
                UnconfirmedContact = new List<CustomizedPerson>();
                FirstRetryInFindContact = true;
                ConfirmedContact = new CustomizedPerson();
            }

            public List<string> ContactsNameList { get; set; }

            public List<EventModel.Attendee> Contacts { get; set; }

            public int ConfirmContactsNameIndex { get; set; }

            public List<CustomizedPerson> UnconfirmedContact { get; set; }

            public bool FirstRetryInFindContact { get; set; }

            public CustomizedPerson ConfirmedContact { get; set; }

            public int ShowContactsIndex { get; set; }

            public string CurrentContactName { get; set; }

            public void Clear()
            {
                CurrentContactName = string.Empty;
                ContactsNameList.Clear();
                Contacts.Clear();
                ConfirmContactsNameIndex = 0;
                ShowContactsIndex = 0;
                UnconfirmedContact.Clear();
                FirstRetryInFindContact = true;
                ConfirmedContact = new CustomizedPerson();
            }
        }

        public class CustomizedPerson
        {
            public CustomizedPerson()
            {
            }

            public CustomizedPerson(PersonModel person)
            {
                this.Emails = new List<ScoredEmailAddress>();
                person.Emails.ToList().ForEach(e => this.Emails.Add(new ScoredEmailAddress() { Address = e }));
                this.DisplayName = person.DisplayName;
                this.UserPrincipalName = person.UserPrincipalName;
            }

            public List<ScoredEmailAddress> Emails { get; set; }

            public string DisplayName { get; set; }

            public string UserPrincipalName { get; set; }
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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Graph;
using static CalendarSkill.Models.CreateEventStateModel;

namespace CalendarSkill.Models
{
    public class CalendarSkillState
    {
        public UserInformation UserInfo { get; set; } = new UserInformation();

        public Luis.CalendarLuis LuisResult { get; set; }

        public Luis.General GeneralLuisResult { get; set; }

        public string APIToken { get; set; }

        public int PageSize { get; set; } = 0;

        public EventSource EventSource { get; set; } = EventSource.Other;

        //public List<EventModel> FocusedMeetings_temp { get; set; }

        public MeetingInformation MeetingInfor { get; set; } = new MeetingInformation();

        public ShowMeetingInformation ShowMeetingInfor { get; set; } = new ShowMeetingInformation();

        public UpdateMeetingInformation UpdateMeetingInfor { get; set; } = new UpdateMeetingInformation();

        // todo: move these to options
        //public bool FirstRetryInFindContact { get; set; }

        //public bool FirstEnterFindContact { get; set; }

        //public bool IsActionFromSummary { get; set; }

        // merge with focused meeting
        //public List<EventModel> ConfirmedMeeting { get; set; }

        public TimeZoneInfo GetUserTimeZone()
        {
            if ((UserInfo != null) && (UserInfo.Timezone != null))
            {
                return UserInfo.Timezone;
            }

            return TimeZoneInfo.Local;
        }

        public void Clear()
        {
            UserInfo = new UserInformation();
            LuisResult = null;
            GeneralLuisResult = null;
            APIToken = null;
            PageSize = 0;
            EventSource = EventSource.Other;
            MeetingInfor.Clear();
            ShowMeetingInfor.Clear();
            UpdateMeetingInfor.Clear();
        }

        public class UserInformation
        {
            public string Name { get; set; }

            public TimeZoneInfo Timezone { get; set; }

            public double Latitude { get; set; }

            public double Longitude { get; set; }
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

        public class MeetingInformation
        {
            public FindContactInformation ContactInfor { get; set; } = new FindContactInformation();

            public string Title { get; set; }

            public string Content { get; set; }

            // user time zone
            public List<DateTime> StartDate { get; set; } = new List<DateTime>();

            // user time zone
            public List<DateTime> StartTime { get; set; } = new List<DateTime>();

            // UTC
            public DateTime? StartDateTime { get; set; }

            public string StartDateString { get; set; }

            // user time zone
            public List<DateTime> EndDate { get; set; } = new List<DateTime>();

            // user time zone
            public List<DateTime> EndTime { get; set; } = new List<DateTime>();

            // UTC
            public DateTime? EndDateTime { get; set; }

            // the order reference, such as 'next'
            public string OrderReference { get; set; }

            public string Location { get; set; }

            public int Duration { get; set; }

            // if the utterance contains any detail like "create a meeting at 4 PM"
            public bool CreateHasDetail { get; set; }

            public RecreateEventState? RecreateState { get; set; }

            public void Clear()
            {
                ContactInfor.Clear();
                Title = null;
                Content = null;
                StartDate.Clear();
                StartTime.Clear();
                StartDateTime = null;
                StartDateString = null;
                EndDate.Clear();
                EndTime.Clear();
                EndDateTime = null;
                OrderReference = null;
                Location = null;
                Duration = 0;
                CreateHasDetail = false;
                RecreateState = null;
            }

            public void ClearLocation()
            {
                Location = null;
                CreateHasDetail = true;
                RecreateState = RecreateEventState.Location;
            }

            public void ClearParticipants()
            {
                ContactInfor.Clear();
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

            public void ClearTimes()
            {
                StartDate.Clear();
                StartDateString = null;
                StartTime.Clear();
                StartDateTime = null;
                EndDate.Clear();
                EndTime.Clear();
                EndDateTime = null;
                Duration = 0;
                CreateHasDetail = true;
                RecreateState = RecreateEventState.Time;
            }

            public void ClearEndTimesAndDuration()
            {
                EndDate.Clear();
                EndTime.Clear();
                EndDateTime = null;
                Duration = 0;
                CreateHasDetail = true;
                RecreateState = RecreateEventState.Duration;
            }
        }

        public class FindContactInformation
        {
            public List<string> ContactsNameList { get; set; } = new List<string>();

            public List<EventModel.Attendee> Contacts { get; set; } = new List<EventModel.Attendee>();

            public int ConfirmContactsNameIndex { get; set; } = 0;

            public List<CustomizedPerson> UnconfirmedContact { get; set; } = new List<CustomizedPerson>();

            // todo: move
            public bool FirstRetryInFindContact { get; set; }

            public CustomizedPerson ConfirmedContact { get; set; } = new CustomizedPerson();

            public int ShowContactsIndex { get; set; } = 0;

            public string CurrentContactName { get; set; }

            public void Clear()
            {
                CurrentContactName = string.Empty;
                ContactsNameList.Clear();
                Contacts.Clear();
                ConfirmContactsNameIndex = 0;
                ShowContactsIndex = 0;
                UnconfirmedContact.Clear();
                // todo: remove?
                FirstRetryInFindContact = true;
                ConfirmedContact = new CustomizedPerson();
            }
        }

        public class ShowMeetingInformation
        {
            public ShowMeetingInformation()
            {
                AskParameterContent = null;
                TotalConflictCount = 0;
                FilterMeetingKeyWord = null;
            }

            public string AskParameterContent { get; set; }

            public int TotalConflictCount { get; set; }

            public string FilterMeetingKeyWord { get; set; }

            public int ShowEventIndex { get; set; } = 0;

            public int UserSelectIndex { get; set; } = -1;

            public List<EventModel> ShowingMeetings { get; set; } = new List<EventModel>();

            public List<EventModel> FocusedEvents { get; set; } = new List<EventModel>();

            public void Clear()
            {
                AskParameterContent = null;
                TotalConflictCount = 0;
                FilterMeetingKeyWord = null;
                ShowEventIndex = 0;
                ShowingMeetings.Clear();
                FocusedEvents.Clear();
            }
        }

        public class UpdateMeetingInformation
        {
            // user time zone
            public List<DateTime> OriginalStartDate { get; set; } = new List<DateTime>();

            // user time zone
            public List<DateTime> OriginalStartTime { get; set; } = new List<DateTime>();

            // user time zone
            public List<DateTime> OriginalEndDate { get; set; } = new List<DateTime>();

            // user time zone
            public List<DateTime> OriginalEndTime { get; set; } = new List<DateTime>();

            // user time zone
            public List<DateTime> NewStartDate { get; set; } = new List<DateTime>();

            // user time zone
            public List<DateTime> NewStartTime { get; set; } = new List<DateTime>();

            // user time zone
            public List<DateTime> NewEndDate { get; set; } = new List<DateTime>();

            // user time zone
            public List<DateTime> NewEndTime { get; set; } = new List<DateTime>();

            // UTC
            public DateTime? NewStartDateTime { get; set; }

            public int MoveTimeSpan { get; set; }

            public string RecurrencePattern { get; set; }

            public void Clear()
            {
                OriginalStartDate.Clear();
                OriginalStartTime.Clear();
                OriginalEndDate.Clear();
                OriginalEndTime.Clear();
                NewStartDate.Clear();
                NewStartTime.Clear();
                NewEndDate.Clear();
                NewEndTime.Clear();
                NewStartDateTime = null;
                MoveTimeSpan = 0;
                RecurrencePattern = null;
            }
        }
    }
}

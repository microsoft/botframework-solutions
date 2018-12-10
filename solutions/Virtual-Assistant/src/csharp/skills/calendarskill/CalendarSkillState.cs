using System;
using System.Collections.Generic;
using Microsoft.Graph;

namespace CalendarSkill
{
    public class CalendarSkillState
    {
        public const int PageSize = 5;

        public CalendarSkillState()
        {
            User = new User();
            UserInfo = new UserInformation();
            Title = null;
            Content = null;
            StartDate = new List<DateTime>();
            StartDateString = null;
            StartTime = new List<DateTime>();
            StartTimeString = null;
            StartDateTime = null;
            EndDate = new List<DateTime>();
            EndTime = new List<DateTime>();
            EndDateTime = null;
            OriginalStartDate = new List<DateTime>();
            OriginalStartTime = new List<DateTime>();
            OriginalEndDate = new List<DateTime>();
            OriginalEndTime = new List<DateTime>();
            Location = null;
            Attendees = new List<EventModel.Attendee>();
            APIToken = null;
            Events = new List<EventModel>();
            NewStartDateTime = null;
            EventSource = EventSource.Other;
            AttendeesNameList = new List<string>();
            ConfirmAttendeesNameIndex = 0;
            DialogName = string.Empty;
            ShowAttendeesIndex = 0;
            ShowEventIndex = 0;
            SummaryEvents = null;
            ReadOutEvents = new List<EventModel>();
            Duration = 0;
            MoveTimeSpan = 0;
            AskParameterContent = string.Empty;
            RecursiveToken = string.Empty;
        }

        public User User { get; set; }

        public UserInformation UserInfo { get; set; } = new UserInformation();

        public Luis.Calendar LuisResult { get; set; }

        public Luis.General GeneralLuisResult { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

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

        // user time zone
        public List<DateTime> OriginalStartDate { get; set; }

        // user time zone
        public List<DateTime> OriginalStartTime { get; set; }

        // user time zone
        public List<DateTime> OriginalEndDate { get; set; }

        // user time zone
        public List<DateTime> OriginalEndTime { get; set; }

        // the order reference, such as 'next'
        public string OrderReference { get; set; }

        // UTC
        public DateTime? EndDateTime { get; set; }

        public string Location { get; set; }

        public List<EventModel.Attendee> Attendees { get; set; }

        public string APIToken { get; set; }

        public List<EventModel> Events { get; set; }

        // UTC
        public DateTime? NewStartDateTime { get; set; }

        public EventSource EventSource { get; set; }

        public List<string> AttendeesNameList { get; set; }

        public int ConfirmAttendeesNameIndex { get; set; }

        public string DialogName { get; set; }

        public int ShowAttendeesIndex { get; set; }

        public int ShowEventIndex { get; set; }

        public List<EventModel> SummaryEvents { get; set; }

        public List<EventModel> ReadOutEvents { get; set; }

        public int Duration { get; set; }

        public string StartDateString { get; set; }

        public string StartTimeString { get; set; }

        public int MoveTimeSpan { get; set; }

        public string AskParameterContent { get; set; }

        public string RecursiveToken { get; set; }

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
            User = new User();
            Title = null;
            Content = null;
            StartDate = new List<DateTime>();
            StartDateString = null;
            StartTime = new List<DateTime>();
            StartTimeString = null;
            StartDateTime = null;
            EndDate = new List<DateTime>();
            EndTime = new List<DateTime>();
            EndDateTime = null;
            OriginalStartDate = new List<DateTime>();
            OriginalStartTime = new List<DateTime>();
            OriginalEndDate = new List<DateTime>();
            OriginalEndTime = new List<DateTime>();
            OrderReference = null;
            Location = null;
            Attendees = new List<EventModel.Attendee>();
            APIToken = null;
            Events = new List<EventModel>();
            NewStartDateTime = null;
            EventSource = EventSource.Other;
            AttendeesNameList = new List<string>();
            ConfirmAttendeesNameIndex = 0;
            DialogName = string.Empty;
            ShowAttendeesIndex = 0;
            ShowEventIndex = 0;
            SummaryEvents = null;
            ReadOutEvents = new List<EventModel>();
            Duration = 0;
            MoveTimeSpan = 0;
            AskParameterContent = string.Empty;
            RecursiveToken = string.Empty;
        }

        public class UserInformation
        {
            public string Name { get; set; }

            public TimeZoneInfo Timezone { get; set; }

            public double Latitude { get; set; }

            public double Longitude { get; set; }
        }
    }
}

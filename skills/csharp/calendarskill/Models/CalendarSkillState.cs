// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Luis;
using Microsoft.Graph;
using static CalendarSkill.Models.CreateEventStateModel;

namespace CalendarSkill.Models
{
    public class CalendarSkillState
    {
        public UserInformation UserInfo { get; set; } = new UserInformation();

        public int PageSize { get; set; } = 0;

        public EventSource EventSource { get; set; } = EventSource.Other;

        public CalendarLuis.Intent InitialIntent { get; set; } = CalendarLuis.Intent.None;

        public MeetingInfomation MeetingInfo { get; set; } = new MeetingInfomation();

        public ShowMeetingInfomation ShowMeetingInfo { get; set; } = new ShowMeetingInfomation();

        public UpdateMeetingInfomation UpdateMeetingInfo { get; set; } = new UpdateMeetingInfomation();

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
            EventSource = EventSource.Other;
            InitialIntent = CalendarLuis.Intent.None;
            MeetingInfo.Clear();
            ShowMeetingInfo.Clear();
            UpdateMeetingInfo.Clear();
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

        public class MeetingInfomation
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

            public AvailabilityResult AvailabilityResult { get; set; }

            public bool IsOrgnizerAvailable { get; set; }

            public string MeetingRoomName { get; set; }

            public RoomModel MeetingRoom { get; set; }

            public List<RoomModel> UnconfirmedMeetingRoom { get; set; } = new List<RoomModel>();

            public List<string> IgnoredMeetingRoom { get; set; } = new List<string>();

            public int ShowMeetingRoomIndex { get; set; } = 0;

            public string Building { get; set; }

            public int? FloorNumber { get; set; }

            public bool AllDay { get; set; } = false;

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
                AvailabilityResult = null;
                MeetingRoom = null;
                MeetingRoomName = null;
                UnconfirmedMeetingRoom.Clear();
                IgnoredMeetingRoom.Clear();
                ShowMeetingRoomIndex = 0;
                Building = null;
                FloorNumber = null;
                AllDay = false;
            }

            public void ClearLocationForRecreate()
            {
                if (MeetingRoom != null)
                {
                    IgnoredMeetingRoom.Add(MeetingRoom.DisplayName + StartDateTime.ToString());
                }

                Location = null;
                MeetingRoom = null;
                MeetingRoomName = null;
                CreateHasDetail = true;
                RecreateState = RecreateEventState.Location;
            }

            public void ClearMeetingRoomForRecreate()
            {
                if (MeetingRoom != null)
                {
                    IgnoredMeetingRoom.Add(MeetingRoom.DisplayName + StartDateTime.ToString());
                }

                Location = null;
                MeetingRoom = null;
                MeetingRoomName = null;
                CreateHasDetail = true;
                RecreateState = RecreateEventState.MeetingRoom;
            }

            public void ClearParticipantsForRecreate()
            {
                ContactInfor.Clear();
                CreateHasDetail = true;
                RecreateState = RecreateEventState.Participants;
            }

            public void ClearSubjectForRecreate()
            {
                Title = null;
                CreateHasDetail = true;
                RecreateState = RecreateEventState.Subject;
            }

            public void ClearContentForRecreate()
            {
                Content = null;
                CreateHasDetail = true;
                RecreateState = RecreateEventState.Content;
            }

            public void ClearTimesForRecreate()
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

            public void ClearEndTimesAndDurationForRecreate()
            {
                EndDate.Clear();
                EndTime.Clear();
                EndDateTime = null;
                Duration = 0;
                CreateHasDetail = true;
                RecreateState = RecreateEventState.Duration;
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
            }

            public void ClearTitle()
            {
                Title = null;
            }
        }

        public class FindContactInformation
        {
            public List<string> ContactsNameList { get; set; } = new List<string>();

            public List<EventModel.Attendee> Contacts { get; set; } = new List<EventModel.Attendee>();

            public int ConfirmContactsNameIndex { get; set; } = 0;

            public List<CustomizedPerson> UnconfirmedContact { get; set; } = new List<CustomizedPerson>();

            public Dictionary<string, RelatedEntityInfo> RelatedEntityInfoDict { get; set; } = new Dictionary<string, RelatedEntityInfo>();

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
                RelatedEntityInfoDict.Clear();
                ConfirmedContact = new CustomizedPerson();
            }
        }

        public class RelatedEntityInfo
        {
            public string PronounType { get; set; }

            public string RelationshipName { get; set; }
        }

        public class ShowMeetingInfomation
        {
            public enum SearchMeetingCondition
            {
                /// <summary>
                /// Search meeting by time.
                /// </summary>
                Time,

                /// <summary>
                /// Search meeting by title.
                /// </summary>
                Title,

                /// <summary>
                /// Search meeting by attendee.
                /// </summary>
                Attendee,

                /// <summary>
                /// Search meeting by location.
                /// </summary>
                Location
            }

            public string AskParameterContent { get; set; } = null;

            public int TotalConflictCount { get; set; } = 0;

            public string FilterMeetingKeyWord { get; set; } = null;

            public int ShowEventIndex { get; set; } = 0;

            public int UserSelectIndex { get; set; } = -1;

            public string ShowingCardTitle { get; set; } = null;

            public List<EventModel> ShowingMeetings { get; set; } = new List<EventModel>();

            // be chosen in ShowingMeetings or in update/change status flow
            public List<EventModel> FocusedEvents { get; set; } = new List<EventModel>();

            public SearchMeetingCondition Condition { get; set; }

            public void Clear()
            {
                AskParameterContent = null;
                TotalConflictCount = 0;
                FilterMeetingKeyWord = null;
                ShowEventIndex = 0;
                ShowingMeetings.Clear();
                FocusedEvents.Clear();
                ShowingCardTitle = null;
            }

            public void ClearFocusedEvents()
            {
                FocusedEvents.Clear();
            }
        }

        public class UpdateMeetingInfomation
        {
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

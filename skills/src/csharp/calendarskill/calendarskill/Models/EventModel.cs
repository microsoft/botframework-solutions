// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using CalendarSkill.Utilities;
using Microsoft.Graph;

namespace CalendarSkill.Models
{
    /// <summary>
    /// Source of event.
    /// </summary>
    public enum EventSource
    {
        /// <summary>
        /// Event from Microsoft.
        /// </summary>
        Microsoft = 1,

        /// <summary>
        /// Event from Google.
        /// </summary>
        Google = 2,

        /// <summary>
        /// Event from other.
        /// </summary>
        Other = 0,
    }

    public enum EventStatus
    {
        /// <summary>
        /// None status.
        /// </summary>
        None = 0,

        /// <summary>
        /// Event is accepted.
        /// </summary>
        Accepted = 1,

        /// <summary>
        /// Event is tentative.
        /// </summary>
        Tentative = 2,

        /// <summary>
        /// Event is Cancelled.
        /// </summary>
        Cancelled = 3,

        /// <summary>
        /// Event is not responded yet.
        /// </summary>
        NotResponded = 4,
    }

    /// <summary>
    /// Event mapping entity.
    /// </summary>
    public partial class EventModel
    {
        /// <summary>
        /// The meeting source.
        /// </summary>
        private EventSource source;

        /// <summary>
        /// The event data of MS Graph.
        /// </summary>
        private Microsoft.Graph.Event msftEventData;

        /// <summary>
        /// The event data of Google.
        /// </summary>
        private Google.Apis.Calendar.v3.Data.Event gmailEventData;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventModel"/> class.、
        /// DO NOT USE THIS ONE.
        /// </summary>
        public EventModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventModel"/> class.
        /// </summary>
        /// <param name="source">the event source.</param>
        public EventModel(EventSource source)
        {
            this.source = source;
            switch (this.source)
            {
                case EventSource.Microsoft:
                    msftEventData = new Microsoft.Graph.Event();
                    break;
                case EventSource.Google:
                    gmailEventData = new Google.Apis.Calendar.v3.Data.Event();
                    break;
                default:
                    throw new Exception("Event Type not Defined");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventModel"/> class from MS Graph event.
        /// </summary>
        /// <param name="msftEvent">MS Graph event.</param>
        public EventModel(Microsoft.Graph.Event msftEvent)
        {
            source = EventSource.Microsoft;
            if (msftEvent.OnlineMeetingUrl == string.Empty)
            {
                msftEvent.OnlineMeetingUrl = null;
            }

            msftEventData = msftEvent;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventModel"/> class from Google event.
        /// </summary>
        /// <param name="gmailEvent">Google event.</param>
        public EventModel(Google.Apis.Calendar.v3.Data.Event gmailEvent)
        {
            source = EventSource.Google;
            gmailEventData = gmailEvent;
        }

        public dynamic Value
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftEventData;
                    case EventSource.Google:
                        return gmailEventData;
                    case EventSource.Other:
                        return null;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                if (value is Google.Apis.Calendar.v3.Data.Event)
                {
                    source = EventSource.Google;
                }

                if (value is Microsoft.Graph.Event)
                {
                    source = EventSource.Microsoft;
                }

                switch (source)
                {
                    case EventSource.Microsoft:
                        if (value.OnlineMeetingUrl == string.Empty)
                        {
                            value.OnlineMeetingUrl = null;
                        }

                        msftEventData = value;
                        break;
                    case EventSource.Google:
                        gmailEventData = value;
                        break;
                    case EventSource.Other:
                        throw new Exception("The default event source is not initialized.");
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public string Id
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftEventData.Id;
                    case EventSource.Google:
                        return gmailEventData.Id;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        msftEventData.Id = value;
                        break;
                    case EventSource.Google:
                        gmailEventData.Id = value;
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public string RecurringId
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftEventData.SeriesMasterId;
                    case EventSource.Google:
                        return gmailEventData.RecurringEventId;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        msftEventData.SeriesMasterId = value;
                        break;
                    case EventSource.Google:
                        gmailEventData.RecurringEventId = value;
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public string Title
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftEventData.Subject;
                    case EventSource.Google:
                        return gmailEventData.Summary;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        msftEventData.Subject = value;
                        break;
                    case EventSource.Google:
                        gmailEventData.Summary = value;
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public string Content
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftEventData.Body.Content;
                    case EventSource.Google:
                        return gmailEventData.Description;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        if (msftEventData.Body == null)
                        {
                            msftEventData.Body = new Microsoft.Graph.ItemBody();
                        }

                        msftEventData.Body.Content = value;
                        msftEventData.Body.ContentType = Microsoft.Graph.BodyType.Text;
                        break;
                    case EventSource.Google:
                        gmailEventData.Description = value;
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public string ContentPreview
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftEventData.BodyPreview;
                    case EventSource.Google:
                        return gmailEventData.Description;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        msftEventData.BodyPreview = value;
                        break;
                    case EventSource.Google:
                        gmailEventData.Description = value;
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        /// <summary>
        /// Gets or sets event start time. StartTime must be local time.
        /// </summary>
        /// <value>
        /// Event start time.
        /// </value>
        public DateTime StartTime
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        if (this.TimeZone == TimeZoneInfo.Utc && !msftEventData.Start.DateTime.EndsWith("Z"))
                        {
                            msftEventData.Start.DateTime = msftEventData.Start.DateTime + "Z";
                        }

                        return DateTime.Parse(msftEventData.Start.DateTime).ToUniversalTime();
                    case EventSource.Google:
                        return gmailEventData.Start.DateTime.Value.ToUniversalTime();
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                if (value.Kind != DateTimeKind.Utc)
                {
                    throw new Exception("Model Start Time is not Utc Time");
                }

                switch (source)
                {
                    case EventSource.Microsoft:
                        if (msftEventData.Start == null)
                        {
                            msftEventData.Start = new Microsoft.Graph.DateTimeTimeZone();
                        }

                        msftEventData.Start.DateTime = value.ToString("o");
                        break;
                    case EventSource.Google:
                        if (gmailEventData.Start == null)
                        {
                            gmailEventData.Start = new Google.Apis.Calendar.v3.Data.EventDateTime();
                        }

                        gmailEventData.Start.DateTimeRaw = value.ToString("o");
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        /// <summary>
        /// Gets or sets event end time. EndTime must be local time.
        /// </summary>
        /// <value>
        /// Event end time.
        /// </value>
        public DateTime EndTime
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        if (this.TimeZone == TimeZoneInfo.Utc && !msftEventData.End.DateTime.EndsWith("Z"))
                        {
                            msftEventData.End.DateTime = msftEventData.End.DateTime + "Z";
                        }

                        return DateTime.Parse(msftEventData.End.DateTime).ToUniversalTime();
                    case EventSource.Google:
                        return gmailEventData.End.DateTime.Value.ToUniversalTime();
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                if (value.Kind != DateTimeKind.Utc)
                {
                    throw new Exception("Model End Time is not Utc Time");
                }

                switch (source)
                {
                    case EventSource.Microsoft:
                        if (msftEventData.End == null)
                        {
                            msftEventData.End = new Microsoft.Graph.DateTimeTimeZone();
                        }

                        msftEventData.End.DateTime = value.ToString("o");
                        break;
                    case EventSource.Google:
                        if (gmailEventData.End == null)
                        {
                            gmailEventData.End = new Google.Apis.Calendar.v3.Data.EventDateTime();
                        }

                        gmailEventData.End.DateTimeRaw = value.ToString("o");
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public TimeZoneInfo TimeZone
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return TimeZoneInfo.FindSystemTimeZoneById(msftEventData.Start.TimeZone);
                    case EventSource.Google:
                        if (gmailEventData.Start.TimeZone == null)
                        {
                            return TimeZoneInfo.Utc;
                        }

                        return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneConverter.IanaToWindows(gmailEventData.Start.TimeZone));
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        if (msftEventData.Start == null)
                        {
                            msftEventData.Start = new Microsoft.Graph.DateTimeTimeZone();
                        }

                        if (msftEventData.End == null)
                        {
                            msftEventData.End = new Microsoft.Graph.DateTimeTimeZone();
                        }

                        msftEventData.Start.TimeZone = value.Id;
                        msftEventData.End.TimeZone = value.Id;
                        break;
                    case EventSource.Google:
                        if (gmailEventData.Start == null)
                        {
                            gmailEventData.Start = new Google.Apis.Calendar.v3.Data.EventDateTime();
                        }

                        if (gmailEventData.End == null)
                        {
                            gmailEventData.End = new Google.Apis.Calendar.v3.Data.EventDateTime();
                        }

                        gmailEventData.Start.TimeZone = TimeZoneConverter.WindowsToIana(value.Id);
                        gmailEventData.End.TimeZone = TimeZoneConverter.WindowsToIana(value.Id);
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public string Location
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftEventData.Location?.DisplayName;
                    case EventSource.Google:
                        return gmailEventData.Location;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        if (msftEventData.Location == null)
                        {
                            msftEventData.Location = new Microsoft.Graph.Location();
                        }

                        msftEventData.Location.DisplayName = value;
                        break;
                    case EventSource.Google:
                        gmailEventData.Location = value;
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public List<Attendee> Attendees
        {
            get
            {
                var attendees = new List<Attendee>();
                switch (source)
                {
                    case EventSource.Microsoft:
                        if (msftEventData.Attendees != null)
                        {
                            foreach (var attendee in msftEventData.Attendees)
                            {
                                attendees.Add(
                                    new Attendee
                                    {
                                        Address = attendee.EmailAddress.Address,
                                        DisplayName = attendee.EmailAddress.Name,
                                    });
                            }
                        }

                        return attendees;
                    case EventSource.Google:
                        if (gmailEventData.Attendees != null)
                        {
                            foreach (var attendee in gmailEventData.Attendees)
                            {
                                attendees.Add(
                                    new Attendee
                                    {
                                        Address = attendee.Email,
                                        DisplayName = attendee.DisplayName,
                                    });
                            }
                        }

                        return attendees;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                var attendees = value;
                switch (source)
                {
                    case EventSource.Microsoft:
                        var msftAttendees = new List<Microsoft.Graph.Attendee>();
                        foreach (var attendee in attendees)
                        {
                            var ms_attendee = new Microsoft.Graph.Attendee()
                            {
                                EmailAddress = new Microsoft.Graph.EmailAddress
                                {
                                    Name = attendee.DisplayName,
                                    Address = attendee.Address,
                                },
                                Type = Microsoft.Graph.AttendeeType.Required,
                            };
                            msftAttendees.Add(ms_attendee);
                        }

                        msftEventData.Attendees = msftAttendees;
                        break;
                    case EventSource.Google:
                        var gmailAttendees = new List<Google.Apis.Calendar.v3.Data.EventAttendee>();
                        foreach (var attendee in attendees)
                        {
                            var gmail_attendee = new Google.Apis.Calendar.v3.Data.EventAttendee
                            {
                                Email = attendee.Address,
                            };
                            gmailAttendees.Add(gmail_attendee);
                        }

                        gmailEventData.Attendees = gmailAttendees;
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public bool IsOrganizer
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftEventData.IsOrganizer.Value;
                    case EventSource.Google:
                        return gmailEventData.Organizer.Self.HasValue && gmailEventData.Organizer.Self.Value;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        msftEventData.IsOrganizer = value;
                        break;
                    case EventSource.Google:
                        // todo check google ones
                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public string OnlineMeetingUrl
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        if (msftEventData.OnlineMeetingUrl == string.Empty)
                        {
                            return null;
                        }

                        return msftEventData.OnlineMeetingUrl;
                    case EventSource.Google:
                        return null;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public bool? IsCancelled
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftEventData.IsCancelled;
                    case EventSource.Google:
                        return gmailEventData.Status.Equals("cancelled");
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public bool IsAccepted
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftEventData.ResponseStatus.Response == ResponseType.Accepted ||
                            msftEventData.ResponseStatus.Response == ResponseType.Organizer ||
                            (msftEventData.IsOrganizer ?? false);
                    case EventSource.Google:
                        return gmailEventData.Status.Equals("confirmed");
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public EventStatus Status
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        switch (msftEventData.ResponseStatus.Response)
                        {
                            case ResponseType.Organizer:
                            case ResponseType.Accepted:
                                return EventStatus.Accepted;
                            case ResponseType.TentativelyAccepted:
                                return EventStatus.Tentative;
                            case ResponseType.Declined:
                                return EventStatus.Cancelled;
                            case ResponseType.NotResponded:
                                return EventStatus.NotResponded;
                            default:
                                return EventStatus.None;
                        }

                    case EventSource.Google:
                        if (gmailEventData.Attendees == null)
                        {
                            return EventStatus.None;
                        }

                        foreach (var attendee in gmailEventData.Attendees)
                        {
                            if (attendee.Self.HasValue && attendee.Self.Value)
                            {
                                switch (attendee.ResponseStatus)
                                {
                                    case GoogleAttendeeStatus.Accepted:
                                        return EventStatus.Accepted;
                                    case GoogleAttendeeStatus.Tentative:
                                        return EventStatus.Tentative;
                                    case GoogleAttendeeStatus.Declined:
                                        return EventStatus.Cancelled;
                                    case GoogleAttendeeStatus.NeedsAction:
                                        return EventStatus.NotResponded;
                                    default:
                                        return EventStatus.None;
                                }
                            }
                        }

                        return EventStatus.None;

                    default:
                        throw new Exception("Event Type not Defined");
                }
            }

            set
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        switch (value)
                        {
                            case EventStatus.Accepted:
                                msftEventData.ResponseStatus.Response = ResponseType.Accepted;
                                break;
                            case EventStatus.Tentative:
                                msftEventData.ResponseStatus.Response = ResponseType.TentativelyAccepted;
                                break;
                            case EventStatus.Cancelled:
                                msftEventData.ResponseStatus.Response = ResponseType.Declined;
                                break;
                            default:
                                break;
                        }

                        break;
                    case EventSource.Google:
                        if (gmailEventData.Attendees == null)
                        {
                            return;
                        }

                        foreach (var attendee in gmailEventData.Attendees)
                        {
                            if (attendee.Self.HasValue && attendee.Self.Value)
                            {
                                switch (value)
                                {
                                    case EventStatus.Accepted:
                                        attendee.ResponseStatus = GoogleAttendeeStatus.Accepted;
                                        break;
                                    case EventStatus.Tentative:
                                        attendee.ResponseStatus = GoogleAttendeeStatus.Tentative;
                                        break;
                                    case EventStatus.Cancelled:
                                        attendee.ResponseStatus = GoogleAttendeeStatus.Declined;
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }

                        break;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public bool? IsAllDay
        {
            get
            {
                switch (source)
                {
                    case EventSource.Microsoft:
                        return msftEventData.IsAllDay;
                    case EventSource.Google:
                        return gmailEventData.Start.Date != null;
                    default:
                        throw new Exception("Event Type not Defined");
                }
            }
        }

        public bool IsConflict { get; set; }

        public EventSource Source
        {
            get => source;

            set => source = value;
        }

        public static bool IsSameDate(DateTime dateTime1, DateTime dateTime2)
        {
            return dateTime1.Year == dateTime2.Year && dateTime1.Month == dateTime2.Month && dateTime1.Day == dateTime2.Day;
        }

        public CalendarItemCardData ToAdaptiveCardData(TimeZoneInfo timeZone)
        {
            var userStartDateTime = TimeConverter.ConvertUtcToUserTime(StartTime, timeZone);
            var duration = EndTime - StartTime;

            return new CalendarItemCardData
            {
                Event = this
                //Time = userStartDateTime.ToString("H:mm"),
                //TimeColor = IsConflict ? "Attention" : "Dark",
                //Title = Title,
                //Location = Location,
                //Duration = ToDisplayDurationString(),
                //IsSubtle = !IsAccepted
            };
        }

        public string ToDisplayDurationString()
        {
            var t = EndTime.Subtract(StartTime);
            return DisplayHelper.ToDisplayMeetingDuration(t);
        }

        public string ToSpeechDurationString()
        {
            var t = EndTime.Subtract(StartTime);
            return SpeakHelper.ToSpeechMeetingDuration(t);
        }

        public bool ContainsAttendee(string contactName)
        {
            foreach (var attendee in Attendees)
            {
                if (attendee.DisplayName != null && attendee.DisplayName.ToLower().Contains(contactName.ToLower()))
                {
                    return true;
                }

                if (attendee.Address != null && attendee.Address.ToLower().Contains(contactName.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }

        public string SourceString()
        {
            switch (Source)
            {
                case EventSource.Microsoft:
                    return "Microsoft Graph";
                case EventSource.Google:
                    return "Gmail";
                default:
                    return null;
            }
        }

        public class Attendee
        {
            public string Address { get; set; }

            public string DisplayName { get; set; }
        }
    }
}

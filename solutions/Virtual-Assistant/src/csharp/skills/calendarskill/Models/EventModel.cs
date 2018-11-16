// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CalendarSkill
{
    using System;
    using System.Collections.Generic;
    using global::CalendarSkill.Common;

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
                        throw new Exception("Get defaut type, please check");
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
                        // todo check google ones
                        return true;
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

        public EventSource Source
        {
            get => source;

            set => source = value;
        }

        public CalendarCardData ToAdaptiveCardData(TimeZoneInfo timeZone, bool showDate = true)
        {
            var eventItem = this;

            var textString = string.Empty;
            if (eventItem.Attendees.Count > 0)
            {
                textString += string.IsNullOrEmpty(eventItem.Attendees[0].DisplayName) ? eventItem.Attendees[0].Address : eventItem.Attendees[0].DisplayName;
                if (eventItem.Attendees.Count > 1)
                {
                    textString += $" + {eventItem.Attendees.Count - 1} others";
                }
            }

            if (showDate || !IsSameDate(eventItem.StartTime, eventItem.EndTime))
            {
                var startDateString = TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, timeZone).ToString("dd-MM-yyyy");
                var endDateString = TimeConverter.ConvertUtcToUserTime(eventItem.EndTime, timeZone).ToString("dd-MM-yyyy");
                if (IsSameDate(eventItem.StartTime, eventItem.EndTime))
                {
                    textString += $"\n{startDateString}";
                }
                else
                {
                    textString += $"\n{startDateString} - {endDateString}";
                }
            }

            if (eventItem.IsAllDay == true)
            {
                textString += "\nAll Day";
            }
            else
            {
                textString += $"\n{TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, timeZone).ToString("h:mm tt")} - {TimeConverter.ConvertUtcToUserTime(eventItem.EndTime, timeZone).ToString("h:mm tt")}";
            }

            if (eventItem.Location != null)
            {
                textString += $"\n{eventItem.Location}";
            }

            string speakString = string.Empty;
            if (eventItem.IsAllDay == true)
            {
                speakString = $"{eventItem.Title} at {TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, timeZone).ToString("MMMM dd all day")}";
            }
            else
            {
                speakString = $"{eventItem.Title} at {TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, timeZone).ToString("h:mm tt")}";
            }

            return new CalendarCardData
            {
                Title = eventItem.Title,
                Content = textString,
                MeetingLink = eventItem.OnlineMeetingUrl,
                Speak = speakString,
            };
        }

        public static bool IsSameDate(DateTime dateTime1, DateTime dateTime2)
        {
            return dateTime1.Year == dateTime2.Year && dateTime1.Month == dateTime2.Month && dateTime1.Day == dateTime2.Day;
        }

        public string ToDurationString()
        {
            TimeSpan t = EndTime.Subtract(StartTime);
            if (t.TotalHours < 1)
            {
                return t.Minutes == 1 ? $"{t.Minutes} minute" : $"{t.Minutes} minutes";
            }
            else if (t.TotalDays < 1)
            {
                if (t.Minutes == 0)
                {
                    return t.Hours == 1 ? $"{t.Hours} hour" : $"{t.Hours} hours";
                }
                else
                {
                    string result = t.Hours == 1 ? $"{t.Hours} hour" : $"{t.Hours} hours";
                    result += " and ";
                    result += t.Minutes == 1 ? $"{t.Minutes} minute" : $"{t.Minutes} minutes";
                    return result;
                }
            }
            else
            {
                return t.Days == 1 ? $"{t.Days} day" : $"{t.Days} days";
            }
        }

        public class Attendee
        {
            public string Address { get; set; }

            public string DisplayName { get; set; }
        }
    }
}

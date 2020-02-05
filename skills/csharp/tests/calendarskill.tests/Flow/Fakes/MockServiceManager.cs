// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Extensions;
using CalendarSkill.Models;
using CalendarSkill.Services;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;
using Moq;

namespace CalendarSkill.Test.Flow.Fakes
{
    public static class MockServiceManager
    {
        private static readonly List<EventModel> BuildinEvents;
        private static readonly List<PersonModel> BuildinPeoples;
        private static readonly List<PersonModel> BuildinUsers;
        private static readonly AvailabilityResult BuildinAvailabilityResult;
        private static readonly List<bool> BuildinCheckAvailableResult;
        private static Mock<ICalendarService> mockCalendarService;
        private static Mock<IUserService> mockUserService;
        private static Mock<IServiceManager> mockServiceManager;

        static MockServiceManager()
        {
            BuildinEvents = GetFakeEvents();
            BuildinPeoples = GetFakePeoples();
            BuildinUsers = GetFakeUsers();
            BuildinAvailabilityResult = GetFakeAvailabilityResult();
            BuildinCheckAvailableResult = GetFakeCheckAvailable();

            // calendar
            mockCalendarService = new Mock<ICalendarService>();
            mockCalendarService.Setup(service => service.CreateEventAysnc(It.IsAny<EventModel>())).Returns((EventModel body) => Task.FromResult(body));
            mockCalendarService.Setup(service => service.GetUpcomingEventsAsync(null)).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetEventsByTimeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetEventsByStartTimeAsync(It.IsAny<DateTime>())).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetEventsByTitleAsync(It.IsAny<string>())).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.UpdateEventByIdAsync(It.IsAny<EventModel>())).Returns((EventModel body) => Task.FromResult(body));
            mockCalendarService.Setup(service => service.DeleteEventByIdAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            mockCalendarService.Setup(service => service.AcceptEventByIdAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            mockCalendarService.Setup(service => service.DeclineEventByIdAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            mockCalendarService.Setup(service => service.GetUserAvailabilityAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<DateTime>(), It.IsAny<int>())).Returns(Task.FromResult(BuildinAvailabilityResult));
            mockCalendarService.Setup(service => service.CheckAvailable(It.IsAny<List<string>>(), It.IsAny<DateTime>(), It.IsAny<int>())).Returns(Task.FromResult(BuildinCheckAvailableResult));

            // user
            mockUserService = new Mock<IUserService>();
            mockUserService.Setup(service => service.GetPeopleAsync(It.IsAny<string>())).Returns((string name) =>
            {
                if (name == Strings.Strings.ThrowErrorAccessDenied)
                {
                    throw new SkillException(SkillExceptionType.APIAccessDenied, Strings.Strings.ThrowErrorAccessDenied, new Exception());
                }

                return Task.FromResult(BuildinPeoples);
            });
            mockUserService.Setup(service => service.GetUserAsync(It.IsAny<string>())).Returns((string name) =>
            {
                if (name == Strings.Strings.ThrowErrorAccessDenied)
                {
                    throw new SkillException(SkillExceptionType.APIAccessDenied, Strings.Strings.ThrowErrorAccessDenied, new Exception());
                }

                return Task.FromResult(BuildinUsers);
            });
            mockUserService.Setup(service => service.GetContactsAsync(It.IsAny<string>())).Returns((string name) =>
            {
                return Task.FromResult(new List<PersonModel>());
            });
            mockUserService.Setup(service => service.GetMeAsync()).Returns(() =>
            {
                var emailAddressStr = Strings.Strings.DefaultUserEmail;
                var userNameStr = Strings.Strings.DefaultUserName;
                var addressList = new List<ScoredEmailAddress>();
                var emailAddress = new ScoredEmailAddress()
                {
                    Address = emailAddressStr,
                    RelevanceScore = 1,
                };
                addressList.Add(emailAddress);

                var people = new Person()
                {
                    UserPrincipalName = emailAddressStr,
                    ScoredEmailAddresses = addressList,
                    DisplayName = userNameStr,
                };

                return Task.FromResult(new PersonModel(people));
            });

            // manager
            mockServiceManager = new Mock<IServiceManager>();
            mockServiceManager.Setup(manager => manager.InitCalendarService(It.IsAny<string>(), It.IsAny<CalendarSkill.Models.EventSource>())).Returns(mockCalendarService.Object);
            mockServiceManager.Setup(manager => manager.InitUserService(It.IsAny<string>(), It.IsAny<CalendarSkill.Models.EventSource>())).Returns(mockUserService.Object);
        }

        public static IServiceManager GetCalendarService()
        {
            return mockServiceManager.Object;
        }

        public static IServiceManager SetMeetingsToSpecial(List<EventModel> eventList)
        {
            mockCalendarService.Setup(service => service.GetUpcomingEventsAsync(null)).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTimeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByStartTimeAsync(It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTitleAsync(It.IsAny<string>())).Returns(Task.FromResult(eventList));
            return mockServiceManager.Object;
        }

        public static IServiceManager SetMeetingsToNull()
        {
            var eventList = new List<EventModel>();

            mockCalendarService.Setup(service => service.GetUpcomingEventsAsync(null)).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTimeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByStartTimeAsync(It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTitleAsync(It.IsAny<string>())).Returns(Task.FromResult(eventList));
            return mockServiceManager.Object;
        }

        public static IServiceManager SetMeetingsToMultiple(int count)
        {
            var eventList = new List<EventModel>();
            for (var i = 0; i < count; i++)
            {
                eventList.Add(CreateEventModel(suffix: i.ToString()));
            }

            mockCalendarService.Setup(service => service.GetUpcomingEventsAsync(null)).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTimeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByStartTimeAsync(It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTitleAsync(It.IsAny<string>())).Returns(Task.FromResult(eventList));
            return mockServiceManager.Object;
        }

        public static IServiceManager SetMeetingWithMeetingRoom()
        {
            var eventList = new List<EventModel>();
            eventList.Add(CreateEventModel(hasMeetingRoom: true));
            mockCalendarService.Setup(service => service.UpdateEventByIdAsync(It.IsAny<EventModel>())).Returns((EventModel body) =>
            {
                EventModel newEvent = CreateEventModel(hasMeetingRoom: true);
                newEvent.Attendees = body.Attendees;
                return Task.FromResult(newEvent);
            });
            mockCalendarService.Setup(service => service.GetUpcomingEventsAsync(null)).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTimeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByStartTimeAsync(It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTitleAsync(It.IsAny<string>())).Returns(Task.FromResult(eventList));
            return mockServiceManager.Object;
        }

        public static IServiceManager SetRoomAvailability(int count, bool available)
        {
            var result = new List<bool>();
            for (var i = 0; i < count; i++)
            {
                result.Add(available);
            }

            mockCalendarService.Setup(service => service.CheckAvailable(It.IsAny<List<string>>(), It.IsAny<DateTime>(), It.IsAny<int>())).Returns(Task.FromResult(result));
            return mockServiceManager.Object;
        }

        public static IServiceManager SetPeopleToMultiple(int count)
        {
            var peoples = new List<PersonModel>();

            for (var i = 0; i < count; i++)
            {
                var emailAddressStr = string.Format(Strings.Strings.UserEmailAddress, i);
                var userNameStr = string.Format(Strings.Strings.UserName, i);
                var addressList = new List<ScoredEmailAddress>();
                var emailAddress = new ScoredEmailAddress()
                {
                    Address = emailAddressStr,
                    RelevanceScore = 1,
                };
                addressList.Add(emailAddress);

                var people = new Person()
                {
                    UserPrincipalName = emailAddressStr,
                    ScoredEmailAddresses = addressList,
                    DisplayName = userNameStr,
                };

                peoples.Add(new PersonModel(people));
            }

            mockUserService.Setup(service => service.GetPeopleAsync(It.IsAny<string>())).Returns((string name) =>
            {
                if (name == Strings.Strings.ThrowErrorAccessDenied)
                {
                    throw new SkillException(SkillExceptionType.APIAccessDenied, Strings.Strings.ThrowErrorAccessDenied, new Exception());
                }

                var result = new List<PersonModel>();
                foreach (var item in peoples)
                {
                    if (item.DisplayName.Contains(name))
                    {
                        result.Add(item);
                    }
                }

                return Task.FromResult(result);
            });
            mockUserService.Setup(service => service.GetUserAsync(It.IsAny<string>())).Returns((string name) =>
            {
                return Task.FromResult(new List<PersonModel>());
            });
            return mockServiceManager.Object;
        }

        public static IServiceManager SetOnePeopleEmailsToMultiple(int count)
        {
            var peoples = new List<PersonModel>();
            var addressList = new List<ScoredEmailAddress>();

            for (var i = 0; i < count; i++)
            {
                var emailAddressStr = string.Format(Strings.Strings.UserEmailAddress, i);
                var emailAddress = new ScoredEmailAddress()
                {
                    Address = emailAddressStr,
                    RelevanceScore = 1,
                };
                addressList.Add(emailAddress);
            }

            var people = new Person()
            {
                UserPrincipalName = string.Format(Strings.Strings.UserEmailAddress, 0),
                ScoredEmailAddresses = addressList,
                DisplayName = Strings.Strings.DefaultUserName,
            };

            peoples.Add(new PersonModel(people));

            mockUserService.Setup(service => service.GetPeopleAsync(It.IsAny<string>())).Returns((string name) =>
            {
                return Task.FromResult(peoples);
            });
            return mockServiceManager.Object;
        }

        public static IServiceManager SetAllToDefault()
        {
            mockCalendarService.Setup(service => service.GetUpcomingEventsAsync(null)).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetEventsByTimeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetEventsByStartTimeAsync(It.IsAny<DateTime>())).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetEventsByTitleAsync(It.IsAny<string>())).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetUserAvailabilityAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<DateTime>(), It.IsAny<int>())).Returns(Task.FromResult(BuildinAvailabilityResult));
            mockUserService.Setup(service => service.GetPeopleAsync(It.IsAny<string>())).Returns((string name) =>
            {
                if (name == Strings.Strings.ThrowErrorAccessDenied)
                {
                    throw new SkillException(SkillExceptionType.APIAccessDenied, Strings.Strings.ThrowErrorAccessDenied, new Exception());
                }

                return Task.FromResult(BuildinPeoples);
            });
            mockUserService.Setup(service => service.GetUserAsync(It.IsAny<string>())).Returns((string name) =>
            {
                if (name == Strings.Strings.ThrowErrorAccessDenied)
                {
                    throw new SkillException(SkillExceptionType.APIAccessDenied, Strings.Strings.ThrowErrorAccessDenied, new Exception());
                }

                return Task.FromResult(BuildinUsers);
            });

            return mockServiceManager.Object;
        }

        public static IServiceManager SetParticipantNotAvailable()
        {
            mockCalendarService.Setup(service => service.GetUserAvailabilityAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<DateTime>(), It.IsAny<int>())).Returns(() =>
            {
                var result = new AvailabilityResult();
                result.AvailabilityViewList.Add("222222000000");
                result.AvailabilityViewList.Add("000000000000");
                return Task.FromResult(result);
            });
            return mockServiceManager.Object;
        }

        public static IServiceManager SetOrgnizerNotAvailable()
        {
            mockCalendarService.Setup(service => service.GetUserAvailabilityAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<DateTime>(), It.IsAny<int>())).Returns((string userEmail, List<string> users, DateTime startTime, int availabilityViewInterval) =>
            {
                var result = new AvailabilityResult();
                result.AvailabilityViewList.Add("000000000000");
                result.AvailabilityViewList.Add("222222000000");
                result.MySchedule.Add(new EventModel(CalendarSkill.Models.EventSource.Microsoft)
                {
                    Title = Strings.Strings.DefaultEventName,
                    StartTime = startTime,
                    EndTime = startTime.AddHours(1)
                });
                return Task.FromResult(result);
            });
            return mockServiceManager.Object;
        }

        public static IServiceManager SetFloor2NotAvailable()
        {
            mockCalendarService.Setup(service => service.CheckAvailable(It.IsAny<List<string>>(), It.IsAny<DateTime>(), It.IsAny<int>())).Returns((List<string> roomEmails, DateTime startTime, int interval) =>
            {
                List<bool> roomAvailability = new List<bool>();
                foreach (var roomEmail in roomEmails)
                {
                    if (roomEmail == string.Format(Strings.Strings.MeetingRoomEmail, 3) || roomEmail == string.Format(Strings.Strings.MeetingRoomEmail, 4))
                    {
                        roomAvailability.Add(false);
                    }
                    else
                    {
                        roomAvailability.Add(true);
                    }
                }

                return Task.FromResult(roomAvailability);
            });

            return mockServiceManager.Object;
        }

        public static EventModel CreateEventModel(
            EmailAddress[] emailAddress = null,
            string eventName = null,
            string content = null,
            DateTime? startDateTime = null,
            DateTime? endDateTime = null,
            string locationString = null,
            bool isOrganizer = true,
            bool isCancelled = false,
            bool hasMeetingRoom = false,
            string suffix = "")
        {
            var attendees = new List<Attendee>();

            if (emailAddress != null)
            {
                foreach (var email in emailAddress)
                {
                    attendees.Add(new Attendee
                    {
                        EmailAddress = email,
                        Type = AttendeeType.Required,
                    });
                }
            }
            else
            {
                attendees.Add(new Attendee
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = Strings.Strings.DefaultUserEmail,
                        Name = Strings.Strings.DefaultUserName + suffix,
                    },
                    Type = AttendeeType.Required,
                });
            }

            if (hasMeetingRoom)
            {
                attendees.Add(new Attendee
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = Strings.Strings.DefaultMeetingRoomEmail,
                        Name = Strings.Strings.DefaultMeetingRoomName,
                    },
                    Type = AttendeeType.Resource,
                });
            }

            // Event Name
            eventName = eventName ?? (Strings.Strings.DefaultEventName + suffix);

            // Event body
            var body = new ItemBody
            {
                Content = content ?? (Strings.Strings.DefaultContent + suffix),
                ContentType = BodyType.Text,
            };

            // Event start and end time
            // Another example date format: `new DateTime(2017, 12, 1, 9, 30, 0).ToString("o")`
            if (startDateTime == null)
            {
                var now = DateTime.Now;
                var startTime = new DateTime(now.Year, now.Month, now.Day, 18, 0, 0);
                startTime = TimeZoneInfo.ConvertTimeToUtc(startTime);
                startDateTime = startTime.AddDays(1);
            }

            if (endDateTime == null)
            {
                endDateTime = startDateTime.Value.AddHours(1);
            }

            var startTimeTimeZone = new DateTimeTimeZone
            {
                DateTime = startDateTime.Value.ToString("o"),
                TimeZone = TimeZoneInfo.Local.Id,
            };
            var endTimeTimeZone = new DateTimeTimeZone
            {
                DateTime = endDateTime.Value.ToString("o"),
                TimeZone = TimeZoneInfo.Local.Id,
            };

            // Event location
            var location = new Location
            {
                DisplayName = locationString ?? (Strings.Strings.DefaultLocation + suffix),
            };

            // Add the event.
            // await _graphClient.Me.Events.Request().AddAsync
            var createdEvent = new Event
            {
                Subject = eventName,
                Location = location,
                Attendees = attendees,
                Body = body,
                Start = startTimeTimeZone,
                End = endTimeTimeZone,
                IsOrganizer = isOrganizer,
                IsCancelled = isCancelled,
                ResponseStatus = new ResponseStatus() { Response = ResponseType.Organizer }
            };

            return new EventModel(createdEvent);
        }

        private static List<EventModel> GetFakeEvents()
        {
            var events = new List<EventModel>
            {
                CreateEventModel()
            };
            return events;
        }

        private static List<PersonModel> GetFakePeoples()
        {
            var peoples = new List<PersonModel>();
            var addressList = new List<ScoredEmailAddress>();
            var emailAddress = new ScoredEmailAddress()
            {
                Address = Strings.Strings.DefaultUserEmail,
                RelevanceScore = 1,
            };
            addressList.Add(emailAddress);

            var people = new Person()
            {
                UserPrincipalName = Strings.Strings.DefaultUserEmail,
                ScoredEmailAddresses = addressList,
                DisplayName = Strings.Strings.DefaultUserName,
            };

            peoples.Add(new PersonModel(people));
            return peoples;
        }

        private static List<PersonModel> GetFakeUsers()
        {
            var users = new List<PersonModel>();

            var emailAddressStr = Strings.Strings.DefaultUserEmail;
            var user = new User()
            {
                UserPrincipalName = Strings.Strings.DefaultUserEmail,
                Mail = emailAddressStr,
                DisplayName = Strings.Strings.DefaultUserName,
            };

            users.Add(new PersonModel(user.ToPerson()));

            return users;
        }

        private static AvailabilityResult GetFakeAvailabilityResult()
        {
            var availabilityResult = new AvailabilityResult();
            availabilityResult.AvailabilityViewList.Add("000000000000");
            availabilityResult.AvailabilityViewList.Add("000000000000");

            return availabilityResult;
        }

        private static List<bool> GetFakeCheckAvailable()
        {
            List<bool> result = new List<bool> { true, true, true, true, true, true, true, true };
            return result;
        }
    }
}

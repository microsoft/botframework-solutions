using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill;
using CalendarSkill.Extensions;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;
using Moq;

namespace CalendarSkillTest.Flow.Fakes
{
    public static class MockServiceManager
    {
        private static readonly List<EventModel> BuildinEvents;
        private static readonly List<PersonModel> BuildinPeoples;
        private static readonly List<PersonModel> BuildinUsers;
        private static Mock<ICalendarService> mockCalendarService;
        private static Mock<IUserService> mockUserService;
        private static Mock<IServiceManager> mockServiceManager;

        static MockServiceManager()
        {
            BuildinEvents = GetFakeEvents();
            BuildinPeoples = GetFakePeoples();
            BuildinUsers = GetFakeUsers();

            // calendar
            mockCalendarService = new Mock<ICalendarService>();
            mockCalendarService.Setup(service => service.CreateEvent(It.IsAny<EventModel>())).Returns((EventModel body) => Task.FromResult(body));
            mockCalendarService.Setup(service => service.GetUpcomingEvents(null)).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetEventsByTime(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetEventsByStartTime(It.IsAny<DateTime>())).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetEventsByTitle(It.IsAny<string>())).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.UpdateEventById(It.IsAny<EventModel>())).Returns((EventModel body) => Task.FromResult(body));
            mockCalendarService.Setup(service => service.DeleteEventById(It.IsAny<string>())).Returns(Task.CompletedTask);
            mockCalendarService.Setup(service => service.AcceptEventById(It.IsAny<string>())).Returns(Task.CompletedTask);
            mockCalendarService.Setup(service => service.DeclineEventById(It.IsAny<string>())).Returns(Task.CompletedTask);

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

            // manager
            mockServiceManager = new Mock<IServiceManager>();
            mockServiceManager.Setup(manager => manager.InitCalendarService(It.IsAny<string>(), It.IsAny<EventSource>())).Returns(mockCalendarService.Object);
            mockServiceManager.Setup(manager => manager.InitUserService(It.IsAny<string>(), It.IsAny<EventSource>())).Returns(mockUserService.Object);
        }

        public static IServiceManager GetCalendarService()
        {
            return mockServiceManager.Object;
        }

        public static IServiceManager SetMeetingsToSpecial(List<EventModel> eventList)
        {
            mockCalendarService.Setup(service => service.GetUpcomingEvents(null)).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTime(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByStartTime(It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTitle(It.IsAny<string>())).Returns(Task.FromResult(eventList));
            return mockServiceManager.Object;
        }

        public static IServiceManager SetMeetingsToNull()
        {
            List<EventModel> eventList = new List<EventModel>();

            mockCalendarService.Setup(service => service.GetUpcomingEvents(null)).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTime(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByStartTime(It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTitle(It.IsAny<string>())).Returns(Task.FromResult(eventList));
            return mockServiceManager.Object;
        }

        public static IServiceManager SetMeetingsToMultiple(int count)
        {
            List<EventModel> eventList = new List<EventModel>();
            for (int i = 0; i < count; i++)
            {
                eventList.Add(CreateEventModel());
            }

            mockCalendarService.Setup(service => service.GetUpcomingEvents(null)).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTime(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByStartTime(It.IsAny<DateTime>())).Returns(Task.FromResult(eventList));
            mockCalendarService.Setup(service => service.GetEventsByTitle(It.IsAny<string>())).Returns(Task.FromResult(eventList));
            return mockServiceManager.Object;
        }

        public static IServiceManager SetPeopleToMultiple(int count)
        {
            List<PersonModel> peoples = new List<PersonModel>();

            for (int i = 0; i < count; i++)
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

                return Task.FromResult(peoples);
            });
            return mockServiceManager.Object;
        }

        public static IServiceManager SetAllToDefault()
        {
            mockCalendarService.Setup(service => service.GetUpcomingEvents(null)).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetEventsByTime(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetEventsByStartTime(It.IsAny<DateTime>())).Returns(Task.FromResult(BuildinEvents));
            mockCalendarService.Setup(service => service.GetEventsByTitle(It.IsAny<string>())).Returns(Task.FromResult(BuildinEvents));
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

        public static EventModel CreateEventModel(
            EmailAddress[] emailAddress = null,
            string eventName = null,
            string content = null,
            DateTime? startDateTime = null,
            DateTime? endDateTime = null,
            string locationString = null,
            bool isOrganizer = true,
            bool isCancelled = false)
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
                        Name = Strings.Strings.DefaultUserName,
                    },
                    Type = AttendeeType.Required,
                });
            }

            // Event Name
            eventName = eventName ?? Strings.Strings.DefaultEventName;

            // Event body
            var body = new ItemBody
            {
                Content = content ?? Strings.Strings.DefaultContent,
                ContentType = BodyType.Text,
            };

            // Event start and end time
            // Another example date format: `new DateTime(2017, 12, 1, 9, 30, 0).ToString("o")`
            if (startDateTime == null)
            {
                DateTime now = DateTime.Now;
                DateTime startTime = new DateTime(now.Year, now.Month, now.Day, 18, 0, 0);
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
                DisplayName = locationString ?? Strings.Strings.DefaultLocation,
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
            List<EventModel> events = new List<EventModel>();
            events.Add(CreateEventModel());
            return events;
        }

        private static List<PersonModel> GetFakePeoples()
        {
            List<PersonModel> peoples = new List<PersonModel>();
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
            List<PersonModel> users = new List<PersonModel>();

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
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using CalendarSkill.ServiceClients;
using CalendarSkill.ServiceClients.GoogleAPI;
using Microsoft.Bot.Solutions.Skills;

namespace CalendarSkill
{
    public class ServiceManager : IServiceManager
    {
        private ISkillConfiguration _skillConfig;

        public ServiceManager(ISkillConfiguration config)
        {
            _skillConfig = config;
        }

        public IUserService InitUserService(string token, EventSource source)
        {
            IUserService userService = null;
            switch (source)
            {
                case EventSource.Microsoft:
                    var serviceClient = GraphClient.GetAuthenticatedClient(token);
                    userService = new MSGraphUserService(serviceClient);
                    break;
                case EventSource.Google:
                    var googleClient = GoogleClient.GetGoogleClient(_skillConfig);
                    var googlePeopleClient = GooglePeopleService.GetServiceClient(googleClient, token);
                    userService = new GooglePeopleService(googlePeopleClient);
                    break;
                default:
                    throw new Exception("Event Type not Defined");
            }

            return new UserService(userService);
        }

        public ICalendar InitCalendarService(string token, EventSource source)
        {
            ICalendar calendarAPI = null;
            switch (source)
            {
                case EventSource.Microsoft:

                case EventSource.Google:
                    GoogleClient googleClient = GoogleClient.GetGoogleClient(_skillConfig);
                    var googlePeopleClient = GoogleCalendarAPI.GetServiceClient(googleClient, token);
                    calendarAPI = new GoogleCalendarAPI(googlePeopleClient);
                    break;
                default:
                    throw new Exception("Event Type not Defined");
            }

            return new CalendarService(calendarAPI, source);
        }
    }
}

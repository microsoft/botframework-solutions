// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients.GoogleAPI;
using CalendarSkill.ServiceClients.MSGraphAPI;
using Microsoft.Bot.Solutions.Skills;

namespace CalendarSkill.ServiceClients
{
    public class ServiceManager : IServiceManager
    {
        private readonly SkillConfigurationBase _skillConfig;

        public ServiceManager(SkillConfigurationBase config)
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

        public ICalendarService InitCalendarService(string token, EventSource source)
        {
            ICalendarService calendarAPI = null;
            switch (source)
            {
                case EventSource.Microsoft:
                    var serviceClient = GraphClient.GetAuthenticatedClient(token);
                    calendarAPI = new MSGraphCalendarAPI(serviceClient);
                    break;
                case EventSource.Google:
                    var googleClient = GoogleClient.GetGoogleClient(_skillConfig);
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

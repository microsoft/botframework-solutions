// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using CalendarSkill.ServiceClients.GoogleAPI;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;

namespace CalendarSkill
{
    public class ServiceManager : IServiceManager
    {
        private ISkillConfiguration _skillConfig;

        public ServiceManager(ISkillConfiguration config)
        {
            _skillConfig = config;
        }

        public IUserService InitUserService(IGraphServiceClient graphClient, TimeZoneInfo info)
        {
            return new MSGraphUserService(graphClient, info);
        }

        public GoogleClient GetGoogleClient()
        {
            if (_skillConfig == null)
            {
                throw new ArgumentNullException(nameof(_skillConfig));
            }

            _skillConfig.Properties.TryGetValue("googleAppName", out object appName);
            _skillConfig.Properties.TryGetValue("googleClientId", out object clientId);
            _skillConfig.Properties.TryGetValue("googleClientSecret", out object clientSecret);
            _skillConfig.Properties.TryGetValue("googleScopes", out object scopes);

            var googleClient = new GoogleClient
            {
                ApplicationName = appName as string,
                ClientId = clientId as string,
                ClientSecret = clientSecret as string,
                Scopes = (scopes as string).Split(" "),
            };

            return googleClient;
        }

        public ICalendar InitCalendarService(ICalendar calendarAPI, EventSource source)
        {
            return new CalendarService(calendarAPI, source);
        }
    }
}

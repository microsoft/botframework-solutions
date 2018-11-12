// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.Graph;
using System.Collections.Generic;
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

        /// <summary>
        /// Init user service with access token.
        /// </summary>
        /// <param name="token">access token.</param>
        /// <param name="info">user timezone info.</param>
        /// <returns>user service.</returns>
        public IUserService InitUserService(string token, TimeZoneInfo info)
        {
            return new MSGraphUserService(token, info);
        }

        public IUserService InitUserService(IGraphServiceClient graphClient, TimeZoneInfo info)
        {
            return new MSGraphUserService(graphClient, info);
        }

        /// <inheritdoc/>
        public ICalendar InitCalendarService(string token, EventSource source)
        {
            if (_skillConfig == null)
            {
                throw new ArgumentNullException(nameof(_skillConfig));
            }

            _skillConfig.Properties.TryGetValue("googleAppName", out object appName);
            _skillConfig.Properties.TryGetValue("googleClientId", out object clientId);
            _skillConfig.Properties.TryGetValue("googleClientSecret", out object clientSecret);
            _skillConfig.Properties.TryGetValue("googleScopes", out object scopes);

            if (clientId == null || clientSecret == null || scopes == null)
            {
                throw new Exception("Please configure your Google Client settings in appsetting.json.");
            }

            var googleClient = new GoogleClient
            {
                ApplicationName = appName as string,
                ClientId = clientId as string,
                ClientSecret = clientSecret as string,
                Scopes = (scopes as string).Split(" "),
            };

            return new CalendarService(token, source, googleClient);
        }

        public ICalendar InitCalendarService(ICalendar calendarAPI, EventSource source)
        {
            return new CalendarService(calendarAPI, source);
        }
    }
}

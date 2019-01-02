// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using EmailSkill.Model;
using EmailSkill.ServiceClients.GoogleAPI;
using EmailSkill.ServiceClients.MSGraphAPI;
using Microsoft.Bot.Solutions.Skills;

namespace EmailSkill.ServiceClients
{
    public class ServiceManager : IServiceManager
    {
        private SkillConfigurationBase _skillConfig;

        public ServiceManager(SkillConfigurationBase config)
        {
            _skillConfig = config;
        }

        /// <inheritdoc/>
        public IUserService InitUserService(string token, TimeZoneInfo timeZoneInfo, MailSource source)
        {
            switch (source)
            {
                case MailSource.Microsoft:
                    var serviceClient = GraphClient.GetAuthenticatedClient(token, timeZoneInfo);
                    return new MSGraphUserService(serviceClient, timeZoneInfo);
                case MailSource.Google:
                    var googleClient = GoogleClient.GetGoogleClient(_skillConfig);
                    var googlePeopleClient = GooglePeopleService.GetServiceClient(googleClient, token);
                    return new GooglePeopleService(googlePeopleClient);
                default:
                    throw new Exception("Event Type not Defined");
            }
        }

        /// <inheritdoc/>
        public IMailService InitMailService(string token, TimeZoneInfo timeZoneInfo, MailSource source)
        {
            switch (source)
            {
                case MailSource.Microsoft:
                    var serviceClient = GraphClient.GetAuthenticatedClient(token, timeZoneInfo);
                    return new MSGraphMailAPI(serviceClient, timeZoneInfo);
                case MailSource.Google:
                    var googleClient = GoogleClient.GetGoogleClient(_skillConfig);
                    var googleServiceClient = GMailService.GetServiceClient(googleClient, token);
                    return new GMailService(googleServiceClient);
                default:
                    throw new Exception("Event Type not Defined");
            }
        }
    }
}
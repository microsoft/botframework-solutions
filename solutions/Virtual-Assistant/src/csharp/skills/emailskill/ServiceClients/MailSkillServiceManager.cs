// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using EmailSkill.ServiceClients.GoogleAPI;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;

namespace EmailSkill
{
    public class MailSkillServiceManager : IMailSkillServiceManager
    {
        private ISkillConfiguration _skillConfig;

        public MailSkillServiceManager(ISkillConfiguration config)
        {
            _skillConfig = config;
        }

        /// <inheritdoc/>
        public IUserService InitUserService(string token, TimeZoneInfo timeZoneInfo, MailSource source)
        {
            switch (source)
            {
                case MailSource.Microsoft:
                    IGraphServiceClient serviceClient = GraphClientHelper.GetAuthenticatedClient(token, timeZoneInfo);
                    return new MSGraphUserService(serviceClient, timeZoneInfo);
                case MailSource.Google:
                    GoogleClient googleClient = GoogleClientHelper.GetAuthenticatedClient(_skillConfig);
                    return new GooglePeopleService(googleClient, token);
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
                    IGraphServiceClient serviceClient = GraphClientHelper.GetAuthenticatedClient(token, timeZoneInfo);
                    return new MSGraphMailAPI(serviceClient, timeZoneInfo);
                case MailSource.Google:
                    GoogleClient googleClient = GoogleClientHelper.GetAuthenticatedClient(_skillConfig);
                    return new GMailService(googleClient, token);
                default:
                    throw new Exception("Event Type not Defined");
            }
        }
    }
}

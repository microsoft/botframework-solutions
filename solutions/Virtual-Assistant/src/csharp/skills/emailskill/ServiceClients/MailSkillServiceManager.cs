// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Graph;

namespace EmailSkill
{
    public class MailSkillServiceManager : IMailSkillServiceManager
    {
        /// <inheritdoc/>
        public IUserService InitUserService(string token, TimeZoneInfo timeZoneInfo)
        {
            IGraphServiceClient serviceClient = GraphClientHelper.GetAuthenticatedClient(token, timeZoneInfo);
            return new UserService(serviceClient, timeZoneInfo);
        }

        /// <inheritdoc/>
        public IMailService InitMailService(string token, TimeZoneInfo timeZoneInfo)
        {
            IGraphServiceClient serviceClient = GraphClientHelper.GetAuthenticatedClient(token, timeZoneInfo);
            return new MailService(serviceClient, timeZoneInfo);
        }
    }
}

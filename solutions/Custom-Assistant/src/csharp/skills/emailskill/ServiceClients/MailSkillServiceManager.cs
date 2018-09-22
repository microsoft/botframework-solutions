// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace EmailSkill
{
    public class MailSkillServiceManager : IMailSkillServiceManager
    {
        /// <inheritdoc/>
        public IUserService InitUserService(string token, TimeZoneInfo timeZoneInfo)
        {
            return new UserService(token, timeZoneInfo);
        }

        /// <inheritdoc/>
        public IMailService InitMailService(string token, TimeZoneInfo timeZoneInfo)
        {
            return new MailService(token, timeZoneInfo);
        }
    }
}

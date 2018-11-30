// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace EmailSkill
{
    public interface IServiceManager
    {
        IUserService InitUserService(string token, TimeZoneInfo timeZoneInfo, MailSource source);

        IMailService InitMailService(string token, TimeZoneInfo timeZoneInfo, MailSource source);
    }
}

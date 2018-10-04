// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace EmailSkill
{
    public interface IMailSkillServiceManager
    {
        IUserService InitUserService(string token, TimeZoneInfo timeZoneInfo);

        IMailService InitMailService(string token, TimeZoneInfo timeZoneInfo);
    }
}

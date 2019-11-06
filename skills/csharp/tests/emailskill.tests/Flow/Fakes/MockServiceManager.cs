// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using EmailSkill.Models;
using EmailSkill.Services;

namespace EmailSkill.Tests.Flow.Fakes
{
    public class MockServiceManager : IServiceManager
    {
        public MockServiceManager()
        {
            MailService = new MockMailService();
            UserService = new MockUserService();
        }

        public MockMailService MailService { get; set; }

        public MockUserService UserService { get; set; }

        public IMailService InitMailService(string token, TimeZoneInfo timeZoneInfo, MailSource mailSource)
        {
            return MailService;
        }

        public IUserService InitUserService(string token, TimeZoneInfo timeZoneInfo, MailSource mailSource)
        {
            return UserService;
        }
    }
}
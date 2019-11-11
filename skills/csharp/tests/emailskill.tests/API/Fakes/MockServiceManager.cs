// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using EmailSkill.Models;
using EmailSkill.Services;
using EmailSkill.Services.MSGraphAPI;
using EmailSkill.Tests.API.Fakes.MSGraph;
using Microsoft.Graph;

namespace EmailSkill.Tests.API.Fakes
{
    public class MockServiceManager : IServiceManager
    {
        public IMailService InitMailService(string token, TimeZoneInfo timeZoneInfo, MailSource source)
        {
            var mockGraphServiceClient = new MockGraphServiceClient();
            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphMailAPI mailService = new MSGraphMailAPI(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            return mailService;
        }

        public IUserService InitUserService(string token, TimeZoneInfo timeZoneInfo, MailSource source)
        {
            var mockGraphServiceClient = new MockGraphServiceClient();
            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphUserService userService = new MSGraphUserService(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            return userService;
        }
    }
}
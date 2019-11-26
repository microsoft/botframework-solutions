// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Services;
using Moq;

namespace CalendarSkill.Test.API.Fakes.MockBaseClient
{
    public static class MockBaseUserClient
    {
        private static Mock<IUserService> mockBaseUserService;

        static MockBaseUserClient()
        {
            mockBaseUserService = new Mock<IUserService>();
            mockBaseUserService.Setup(service => service.GetPeopleAsync(It.IsAny<string>())).Returns((string name) =>
            {
                return Task.FromResult(new List<PersonModel>());
            });
            mockBaseUserService.Setup(service => service.GetUserAsync(It.IsAny<string>())).Returns((string name) =>
            {
                return Task.FromResult(new List<PersonModel>());
            });
            mockBaseUserService.Setup(service => service.GetContactsAsync(It.IsAny<string>())).Returns((string name) =>
            {
                return Task.FromResult(new List<PersonModel>());
            });
        }

        public static IUserService GetUserService()
        {
            return mockBaseUserService.Object;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Services;
using CalendarSkill.Services.GoogleAPI;
using CalendarSkill.Test.API.Fakes.MockGoogleClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkill.Test.API
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class UserServiceGoogleTests
    {
        private static IUserService userService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        [TestInitialize]
        public void TestInit()
        {
            userService = new UserService(new GooglePeopleService(MockGoogleUserClient.GetPeopleService()));
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public async Task GetPeopleAsyncTest()
        {
            List<PersonModel> result = await userService.GetPeopleAsync("Doe");
            Assert.IsTrue(result.Count == 2);
        }

        [TestMethod]
        public async Task GetUserAsyncTest()
        {
            List<PersonModel> result = await userService.GetUserAsync("Doe");
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod]
        public async Task GetContactsAsyncTest()
        {
            List<PersonModel> result = await userService.GetContactsAsync("Doe");
            Assert.IsTrue(result.Count == 0);
        }
    }
}

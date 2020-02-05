// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Services;
using EmailSkill.Services.GoogleAPI;
using EmailSkill.Tests.API.Fakes.Google;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkill.Tests.API.Service
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class GoogleUserServiceTests
    {
        public static IUserService UserService { get; set; }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            UserService = new GooglePeopleService(MockGoogleUserClient.MockGoogleUserService.Object);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        [TestInitialize]
        public void TestInit()
        {
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public async Task GetPeopleAsyncTest()
        {
            List<PersonModel> result = await UserService.GetPeopleAsync("Doe");
            Assert.IsTrue(result.Count == 2);
        }

        [TestMethod]
        public async Task GetUserAsyncTest()
        {
            List<PersonModel> result = await UserService.GetUserAsync("Doe");
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod]
        public async Task GetContactsAsyncTest()
        {
            List<PersonModel> result = await UserService.GetContactsAsync("Doe");
            Assert.IsTrue(result.Count == 0);
        }
    }
}
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using EmailSkill.Dialogs;
using EmailSkill.Models;
using EmailSkill.Services;
using EmailSkill.Tests.API.Fakes;
using EmailSkill.Utilities;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkill.Tests.API.Helper
{
    [TestClass]
    public class StepHelperTests : EmailSkillDialogBase
    {
        private const string DialogId = "test";
        private MockDialogStateAccessor mockDialogStateAccessor;
        private MockEmailStateAccessor mockEmailStateAccessor;

        public StepHelperTests()
            : base(DialogId)
        {
            Services = new BotServices();

            mockEmailStateAccessor = new MockEmailStateAccessor();
            EmailStateAccessor = mockEmailStateAccessor.GetMock().Object;

            mockDialogStateAccessor = new MockDialogStateAccessor();
            DialogStateAccessor = mockDialogStateAccessor.GetMock().Object;

            ServiceManager = new MockServiceManager();
        }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
        }

        [TestMethod]
        public async Task GetNameListStringTest_OneOption()
        {
            // Mock data
            mockEmailStateAccessor.MockEmailSkillState = new EmailSkillState
            {
                FindContactInfor = new EmailSkillState.FindContactInformation()
            };

            mockEmailStateAccessor.MockEmailSkillState.FindContactInfor.Contacts = GetRecipients(1);

            mockEmailStateAccessor.SetMockBehavior();
            EmailStateAccessor = mockEmailStateAccessor.GetMock().Object;

            var nameList = await GetNameListStringAsync(null);

            Assert.AreEqual(nameList, "test0: test0@test.com");
        }

        [TestMethod]
        public async Task GetNameListStringTest_TwoOptions()
        {
            // Mock data
            mockEmailStateAccessor.MockEmailSkillState = new EmailSkillState
            {
                FindContactInfor = new EmailSkillState.FindContactInformation()
            };

            mockEmailStateAccessor.MockEmailSkillState.FindContactInfor.Contacts = GetRecipients(2);

            mockEmailStateAccessor.SetMockBehavior();
            EmailStateAccessor = mockEmailStateAccessor.GetMock().Object;

            var nameList = await GetNameListStringAsync(null);

            Assert.AreEqual(nameList, "test0: test0@test.com and test1: test1@test.com");
        }

        [TestMethod]
        public async Task GetNameListStringTest_ThreeOptions()
        {
            // Mock data
            mockEmailStateAccessor.MockEmailSkillState = new EmailSkillState
            {
                FindContactInfor = new EmailSkillState.FindContactInformation()
            };

            mockEmailStateAccessor.MockEmailSkillState.FindContactInfor.Contacts = GetRecipients(3);

            mockEmailStateAccessor.SetMockBehavior();
            EmailStateAccessor = mockEmailStateAccessor.GetMock().Object;

            var nameList = await GetNameListStringAsync(null);

            Assert.AreEqual(nameList, "test0: test0@test.com, test1: test1@test.com and test2: test2@test.com");
        }

        [TestMethod]
        public void FormatRecipientListTest()
        {
            var personData = GetPersonLists(0, 5);
            var contactData = GetPersonLists(1, 6);
            personData.AddRange(contactData);

            var originPersonList = personData;
            var originUserList = GetPersonLists(2, 7);

            (var personList, var userList) = DisplayHelper.FormatRecipientList(originPersonList, originUserList);

            Assert.AreEqual(personList.Count, 6);
            Assert.AreEqual(userList.Count, 1);
        }

        private List<Recipient> GetRecipients(int count)
        {
            var result = new List<Recipient>();

            for (var i = 0; i < count; i++)
            {
                var recipient = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Name = "test" + i.ToString(),
                        Address = "test" + i.ToString() + "@test.com"
                    }
                };

                result.Add(recipient);
            }

            return result;
        }

        private List<PersonModel> GetPersonLists(int start, int end)
        {
            var result = new List<PersonModel>();

            for (var i = start; i < end; i++)
            {
                var emailList = new List<string>
                {
                    "test" + i.ToString() + "@test.com"
                };

                var person = new PersonModel
                {
                    DisplayName = "test" + i.ToString(),
                    Emails = emailList
                };

                result.Add(person);
            }

            return result;
        }
    }
}
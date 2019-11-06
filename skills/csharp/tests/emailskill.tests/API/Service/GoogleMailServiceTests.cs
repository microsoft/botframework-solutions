﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmailSkill.Services;
using EmailSkill.Services.GoogleAPI;
using EmailSkill.Tests.API.Fakes.Google;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkill.Tests.API.Service
{
    [TestClass]
    public class GoogleMailServiceTests
    {
        public static IMailService MailService { get; set; }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var mockGoogleServiceClient = new MockGoogleServiceClient();
            MailService = new GMailService(mockGoogleServiceClient.GetMockGraphServiceClient().Object);
        }

        [TestMethod]
        public async Task ForwardMessageTest()
        {
            List<Recipient> recipients = new List<Recipient>
            {
                new Recipient()
                {
                    EmailAddress = new EmailAddress()
                    {
                        Address = "test@test.com"
                    }
                }
            };

            await MailService.ForwardMessageAsync("1", "Test", recipients);
        }

        [TestMethod]
        public async Task SendMessageTest()
        {
            List<Recipient> recipients = new List<Recipient>
            {
                new Recipient()
                {
                    EmailAddress = new EmailAddress()
                    {
                        Address = "test@test.com"
                    }
                }
            };

            await MailService.SendMessageAsync("test content", "test subject", recipients);
        }

        [TestMethod]
        public async Task ReplyMessageTest()
        {
            await MailService.ReplyToMessageAsync("1", "test content");
        }

        [TestMethod]
        public async Task GetMessagesTest()
        {
            var messageList = await MailService.GetMyMessagesAsync(DateTime.Now, DateTime.Now.AddDays(7), false, false, false, null);
            Assert.AreEqual(messageList.Count, 5);
        }

        [TestMethod]
        public async Task DeleteMessagesTest()
        {
            await MailService.DeleteMessageAsync("1");
        }

        [TestMethod]
        public async Task MartAsReadTest()
        {
            await MailService.MarkMessageAsReadAsync("1");
        }
    }
}
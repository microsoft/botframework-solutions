using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmailSkill;
using EmailSkillTest.API.Fakes;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.API
{
    [TestClass]
    public class GoogleMailServiceTests
    {
        public static IMailService mailService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var mockGoogleServiceClient = new MockGoogleServiceClient();
            mailService = new GMailService(mockGoogleServiceClient.GetMockGraphServiceClient().Object);
        }

        [TestMethod]
        public async Task ForwardMessageTest()
        {
            List<Recipient> recipients = new List<Recipient>();
            recipients.Add(new Recipient()
            {
                EmailAddress = new EmailAddress()
                {
                    Address = "test@test.com"
                }
            });

            await mailService.ForwardMessageAsync("1", "Test", recipients);
        }

        [TestMethod]
        public async Task SendMessageTest()
        {
            List<Recipient> recipients = new List<Recipient>();
            recipients.Add(new Recipient()
            {
                EmailAddress = new EmailAddress()
                {
                    Address = "test@test.com"
                }
            });

            await mailService.SendMessageAsync("test content", "test subject", recipients);
        }

        [TestMethod]
        public async Task ReplyMessageTest()
        {
            await mailService.ReplyToMessageAsync("1", "test content");
        }

        [TestMethod]
        public async Task GetMessagesTest()
        {
            var messageList = await mailService.GetMyMessagesAsync(DateTime.Now, DateTime.Now.AddDays(7), false, false, false, null, 0);
            Assert.AreEqual(messageList.Count, 5);
        }
    }
}

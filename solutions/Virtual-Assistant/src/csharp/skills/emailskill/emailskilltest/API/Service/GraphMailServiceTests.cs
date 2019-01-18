using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmailSkill.ServiceClients.MSGraphAPI;
using EmailSkillTest.API.Fakes.MSGraph;
using Microsoft.Bot.Solutions.Data;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.API.Service
{
    [TestClass]
    public class GraphMailServiceTests
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
        }

        [TestMethod]
        public async Task SendMessageTest()
        {
            // Send a self to self message
            var recipient = new Recipient()
            {
                EmailAddress = new EmailAddress(),
            };
            recipient.EmailAddress.Address = "test@test.com";
            recipient.EmailAddress.Name = "Test Test";

            List<Recipient> recipientList = new List<Recipient>
            {
                recipient
            };

            var mockGraphServiceClient = new MockGraphServiceClient();
            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphMailAPI mailService = new MSGraphMailAPI(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            await mailService.SendMessageAsync("test content", "test subject", recipientList);
        }

        [TestMethod]
        public async Task GetMyMessagesTest()
        {
            IMailFolderMessagesCollectionPage messages = new MailFolderMessagesCollectionPage();
            for (int i = 0; i < 6; i++)
            {
                var message = new Message()
                {
                    Subject = "TestSubject" + i,
                    BodyPreview = "TestBodyPreview" + i,
                    Body = new ItemBody()
                    {
                        Content = "TestBody" + i,
                        ContentType = BodyType.Text,
                    },
                    ReceivedDateTime = DateTime.Now.AddHours(-1),
                    WebLink = "http://www.test.com",
                    Sender = new Recipient()
                    {
                        EmailAddress = new EmailAddress()
                        {
                            Name = "TestSender" + i,
                        },
                    },
                };

                var recipients = new List<Recipient>();
                var recipient = new Recipient()
                {
                    EmailAddress = new EmailAddress(),
                };
                recipient.EmailAddress.Address = i + "test@test.com";
                recipient.EmailAddress.Name = "Test Test";
                recipients.Add(recipient);
                message.ToRecipients = recipients;

                messages.Add(message);
            }

            var mockGraphServiceClient = new MockGraphServiceClient
            {
                MyMessages = messages
            };
            mockGraphServiceClient.SetMockBehavior();
            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphMailAPI mailService = new MSGraphMailAPI(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            List<Message> result = await mailService.GetMyMessagesAsync(DateTime.Today.AddDays(-2), DateTime.Today.AddDays(1), getUnRead: false, isImportant: false, directlyToMe: false, fromAddress: "test@test.com");

            // Test get 0-5 message per page
            Assert.IsTrue(result.Count >= 1);

            // Test ranking correctly by time
            Assert.IsTrue(result[0].Subject == "TestSubject5");
        }

        [TestMethod]
        public async Task ReplyToMessageTest()
        {
            var mockGraphServiceClient = new MockGraphServiceClient();
            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphMailAPI mailService = new MSGraphMailAPI(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            await mailService.ReplyToMessageAsync("1", "test");
        }

        [TestMethod]
        public async Task UpdateMessageTest()
        {
            var mockGraphServiceClient = new MockGraphServiceClient();
            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphMailAPI mailService = new MSGraphMailAPI(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            Message msg = new Message();
            await mailService.UpdateMessage(msg);
        }

        [TestMethod]
        public async Task ForwardMessageTest()
        {
            var mockGraphServiceClient = new MockGraphServiceClient();
            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphMailAPI mailService = new MSGraphMailAPI(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            List<Recipient> recipients = new List<Recipient>();
            await mailService.ForwardMessageAsync("1", "Test", recipients);
        }

        [TestMethod]
        public async Task DeleteMessageTest()
        {
            var mockGraphServiceClient = new MockGraphServiceClient();
            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphMailAPI mailService = new MSGraphMailAPI(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            await mailService.DeleteMessageAsync("1");
        }
    }
}
namespace EmailSkillTest.API
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using EmailSkill;
    using Microsoft.Graph;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class MailServiceTests
    {
        private static MailService mailService;
        private static string testEmailAddress = "test@test.com";

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var mo = new Mock<IGraphServiceClient>();
            mo.Setup(f => f.Me.Request().GetAsync()).Returns(Task.FromResult(new User()));

            var optionList = new List<QueryOption>();
            mo.Setup(f => f.Me.MailFolders.Inbox.Messages.Request(optionList).GetAsync()).Returns(new Task<IMailFolderMessagesCollectionPage>(GetMailsbyOption));

            mo.Setup(f => f.Me.MailFolders.Inbox.Messages.Request().GetAsync()).Returns(new Task<IMailFolderMessagesCollectionPage>(GetMailsbyOption));

            Message email = new Message();
            mo.Setup(f => f.Me.SendMail(email, true).Request(null).PostAsync()).Returns(new Task(Operation));
            mo.Setup(f => f.Me.Messages["1"].ReplyAll(string.Empty).Request(null).PostAsync()).Returns(new Task(Operation));
            //mo.Setup(f => f.Me.Messages["1"].Request().UpdateAsync(email)).Returns(new Task(Update));

            IGraphServiceClient serviceClient = mo.Object;
            mailService = new MailService(serviceClient, timeZoneInfo: TimeZoneInfo.Local);
        }

        private static User GetMe()
        {
            var user = new User();
            user.Mail = "test@test.com";

            return user;
        }

        private static MailFolderMessagesCollectionPage GetMailsbyOption()
        {
            var mails = new MailFolderMessagesCollectionPage();

            return mails;
        }

        private static void Operation()
        {
            return;
        }

        //private static Message Update()
        //{
        //    return new Message();
        //}

        [TestMethod]
        public async Task SendMessageTest()
        {
            // Send a self to self message
            var recipient = new Recipient()
            {
                EmailAddress = new EmailAddress(),
            };
            recipient.EmailAddress.Address = testEmailAddress;
            recipient.EmailAddress.Name = "Test Test";

            List<Recipient> recipientList = new List<Recipient>();
            recipientList.Add(recipient);

            await mailService.SendMessage("test content", "test subject", recipientList);
        }

        [TestMethod]
        public async Task GetMyMessagesTest()
        {
            List<Message> messages = await mailService.GetMyMessages(DateTime.Today, DateTime.Today.AddDays(1), getUnRead: false, isImportant: false, directlyToMe: false, fromAddress: testEmailAddress, skip: 0);
            Assert.IsTrue(messages.Count >= 1);
            Assert.IsTrue(messages.Count <= 5);
        }

        [TestMethod]
        public async Task ReplyToMessageTest()
        {
            await mailService.ReplyToMessage("1", "test");
        }

        [TestMethod]
        public async Task UpdateMessageTest()
        {
            Message msg = new Message();
            await mailService.UpdateMessage(msg);
        }

        [TestMethod]
        public async Task ForwardMessageTest()
        {
            List<Recipient> recipients = new List<Recipient>();
            await mailService.ForwardMessage("1", "Test", recipients);
        }

        [TestMethod]
        public async Task DeleteMessageTest()
        {
            await mailService.DeleteMessage("1");
        }
    }
}

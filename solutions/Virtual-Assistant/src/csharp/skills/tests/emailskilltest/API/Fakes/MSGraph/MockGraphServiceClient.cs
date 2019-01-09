using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using Moq;

namespace EmailSkillTest.API.Fakes.MSGraph
{
    public class MockGraphServiceClient
    {
        private readonly Mock<IGraphServiceClient> mockMailService;

        public MockGraphServiceClient()
        {
            this.mockMailService = new Mock<IGraphServiceClient>();
            this.InitializeDefaultData();
            this.SetMockBehavior();
        }

        public User Me { get; set; }

        public IMailFolderMessagesCollectionPage MyMessages { get; set; }

        public IGraphServiceUsersCollectionPage Users { get; set; }

        public IUserPeopleCollectionPage People { get; set; }

        public IUserContactsCollectionPage Contacts { get; set; }

        public void InitializeDefaultData()
        {
            this.Me = new User();
            Me.Mail = "test@test.com";
            Me.DisplayName = "Test Test";

            this.MyMessages = new MailFolderMessagesCollectionPage();

            this.Users = new GraphServiceUsersCollectionPage();

            this.People = new UserPeopleCollectionPage();

            this.Contacts = new UserContactsCollectionPage();
        }

        public void SetMockBehavior()
        {
            // Mock mail service behavior
            this.MockGetMyMessages();
            this.MockSendMessage();
            this.MockReplyToMessage();
            this.MockUpdateMessage();
            this.MockForwardMessage();
            this.MockDeleteMessage();

            // Mock user service behavior
            this.MockGetUserAsync();
            this.MockGetPeopleAsync();
            this.MockGetContactAsync();
        }

        public Mock<IGraphServiceClient> GetMockGraphServiceClient()
        {
            return this.mockMailService;
        }

        private void MockGetMyMessages()
        {
            this.mockMailService.Setup(f => f.Me.Request().GetAsync()).Returns(Task.FromResult(this.Me));
            this.mockMailService.Setup(f => f.Me.MailFolders.Inbox.Messages.Request(It.IsAny<List<QueryOption>>()).GetAsync()).Returns(Task.FromResult(this.MyMessages));
        }

        private void MockSendMessage()
        {
            this.mockMailService.Setup(f => f.Me.SendMail(It.IsAny<Message>(), true).Request(null).PostAsync()).Returns(Task.CompletedTask);
        }

        private void MockReplyToMessage()
        {
            this.mockMailService.Setup(f => f.Me.Messages[It.IsAny<string>()].ReplyAll(It.IsAny<string>()).Request(null).PostAsync()).Returns(Task.CompletedTask);
        }

        private void MockUpdateMessage()
        {
            this.mockMailService.Setup(f => f.Me.Messages[It.IsAny<string>()].Request().UpdateAsync(It.IsAny<Message>())).Returns(Task.FromResult(new Message()));
        }

        private void MockForwardMessage()
        {
            this.mockMailService.Setup(f => f.Me.Messages[It.IsAny<string>()].Forward(It.IsAny<string>(), It.IsAny<IEnumerable<Recipient>>()).Request(null).PostAsync()).Returns(Task.CompletedTask);
        }

        private void MockDeleteMessage()
        {
            this.mockMailService.Setup(f => f.Me.Messages[It.IsAny<string>()].Request().DeleteAsync()).Returns(Task.CompletedTask);
        }

        private void MockGetUserAsync()
        {
            this.mockMailService.Setup(f => f.Users.Request(It.IsAny<List<QueryOption>>()).GetAsync()).Returns(Task.FromResult(this.Users));
        }

        private void MockGetPeopleAsync()
        {
            this.mockMailService.Setup(f => f.Me.People.Request(It.IsAny<List<QueryOption>>()).GetAsync()).Returns(Task.FromResult(this.People));
        }

        private void MockGetContactAsync()
        {
            this.mockMailService.Setup(f => f.Me.Contacts.Request(It.IsAny<List<QueryOption>>()).GetAsync()).Returns(Task.FromResult(this.Contacts));
        }
    }
}
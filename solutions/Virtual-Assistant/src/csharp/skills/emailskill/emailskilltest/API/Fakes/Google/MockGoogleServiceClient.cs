using Google.Apis.Gmail.v1;
using Moq;
using static Google.Apis.Gmail.v1.UsersResource;
using GmailMessage = Google.Apis.Gmail.v1.Data.Message;

namespace EmailSkillTest.API.Fakes.Google
{
    public class MockGoogleServiceClient
    {
        private readonly Mock<GmailService> mockMailService;
        private readonly Mock<MessagesResource> mockMessagesResource;
        private readonly Mock<UsersResource> mockUsersResource;

        public MockGoogleServiceClient()
        {
            this.mockMailService = new Mock<GmailService>();
            this.mockMessagesResource = new Mock<MessagesResource>(mockMailService.Object);
            this.mockUsersResource = new Mock<UsersResource>(mockMailService.Object);

            this.mockUsersResource.SetupGet(users => users.Messages).Returns(mockMessagesResource.Object);

            this.mockUsersResource.Setup(users => users.GetProfile(It.IsAny<string>())).Returns((string userId) =>
            {
                MockUsersResource.MockGetProfileRequest mockGetProfileRequest = new MockUsersResource.MockGetProfileRequest(this.mockMailService.Object, userId);
                return mockGetProfileRequest;
            });

            this.mockMailService.SetupGet(service => service.Users).Returns(mockUsersResource.Object);

            this.mockMessagesResource.Setup(messages => messages.Send(It.IsAny<GmailMessage>(), It.IsAny<string>())).Returns((GmailMessage body, string userId) =>
            {
                MockMessagesResource.MockSendRequest mockSendRequest = new MockMessagesResource.MockSendRequest(this.mockMailService.Object, body, userId);
                return mockSendRequest;
            });

            this.mockMessagesResource.Setup(messages => messages.Get(It.IsAny<string>(), It.IsAny<string>())).Returns((string userId, string id) =>
            {
                MockMessagesResource.MockGetRequest mockGetRequest = new MockMessagesResource.MockGetRequest(this.mockMailService.Object, userId, id);

                return mockGetRequest;
            });

            this.mockMessagesResource.Setup(messages => messages.List(It.IsAny<string>())).Returns((string userId) =>
            {
                MockMessagesResource.MockListRequest mockListRequest = new MockMessagesResource.MockListRequest(this.mockMailService.Object, userId);

                return mockListRequest;
            });
        }

        public Mock<GmailService> GetMockGraphServiceClient()
        {
            return this.mockMailService;
        }
    }
}
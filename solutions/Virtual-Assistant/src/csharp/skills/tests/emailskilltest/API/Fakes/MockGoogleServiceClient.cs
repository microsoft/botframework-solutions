using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using EmailSkill;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using MimeKit;
using Moq;
using static Google.Apis.Gmail.v1.UsersResource;
using GmailMessage = Google.Apis.Gmail.v1.Data.Message;

namespace EmailSkillTest.API.Fakes
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

    public class MockUsersResource
    {
        public class MockGetProfileRequest : GetProfileRequest, IClientServiceRequest<Profile>
        {
            public MockGetProfileRequest(IClientService service, string userId)
                : base(service, userId)
            {
            }

            public new Profile Execute()
            {
                if (UserId != "me")
                {
                    throw new Exception("User ID not support");
                }

                var profile = new Profile();
                profile.EmailAddress = "test@test.com";

                return profile;
            }
        }
    }

    public class MockMessagesResource
    {
        public class MockSendRequest : MessagesResource.SendRequest, IClientServiceRequest<GmailMessage>
        {
            public MockSendRequest(IClientService service, GmailMessage body, string userId)
                : base(service, body, userId)
            {
                this.Body = body;
            }

            public GmailMessage Body { get; set; }

            public new Task<GmailMessage> ExecuteAsync()
            {
                if (UserId != "me")
                {
                    throw new Exception("User ID not support");
                }

                return Task.FromResult(this.Body);
            }
        }

        public class MockGetRequest : MessagesResource.GetRequest, IClientServiceRequest<GmailMessage>
        {
            public MockGetRequest(IClientService service, string userId, string id)
                : base(service, userId, id)
            {
            }

            public new Task<GmailMessage> ExecuteAsync()
            {
                if (UserId != "me")
                {
                    throw new Exception("User ID not support");
                }

                return Task.FromResult(GmailUtil.GetFakeGmailMessage());
            }
        }

        public class MockListRequest : MessagesResource.ListRequest, IClientServiceRequest<ListMessagesResponse>
        {
            public MockListRequest(IClientService service, string userId)
                : base(service, userId)
            {
            }

            public new ListMessagesResponse Execute()
            {
                var result = new ListMessagesResponse();
                var messageList = GmailUtil.GetFakeGmailMessageList();

                foreach (var message in messageList)
                {
                    result.Messages.Add(message);
                }

                return result;
            }

            public new Task<ListMessagesResponse> ExecuteAsync()
            {
                var result = new ListMessagesResponse();
                result.Messages = GmailUtil.GetFakeGmailMessageList();

                return Task.FromResult(result);
            }
        }
    }

    public class GmailUtil
    {
        public static IList<GmailMessage> GetFakeGmailMessageList(int size = 5)
        {
            var messages = new List<GmailMessage>();

            for (int i = 0; i < size; i++)
            {
                var message = GetFakeGmailMessage(to: "test@test.com" + i);
                messages.Add(message);
            }

            return messages;
        }

        public static GmailMessage GetFakeGmailMessage(
            string from = "test@test.com",
            string to = "test@test.com",
            string subject = "test subject",
            string content = "test content")
        {
            var mess = new MailMessage();
            mess.Subject = subject;
            mess.From = new MailAddress(from);
            mess.To.Add(new MailAddress(to));

            var adds = AlternateView.CreateAlternateViewFromString(content, new System.Net.Mime.ContentType("text/plain"));
            adds.ContentType.CharSet = Encoding.UTF8.WebName;
            mess.AlternateViews.Add(adds);

            var mime = MimeMessage.CreateFromMailMessage(mess);
            var gmailMessage = new GmailMessage()
            {
                Raw = GMailService.Base64UrlEncode(mime.ToString()),
                ThreadId = "1"
            };

            return gmailMessage;
        }
    }
}

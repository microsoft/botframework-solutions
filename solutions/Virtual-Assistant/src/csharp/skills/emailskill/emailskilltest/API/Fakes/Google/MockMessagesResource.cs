using System;
using System.Threading.Tasks;
using EmailSkill.ServiceClients.GoogleAPI;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using static Google.Apis.Gmail.v1.UsersResource;
using GmailMessage = Google.Apis.Gmail.v1.Data.Message;

namespace EmailSkillTest.API.Fakes.Google
{
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

                var mime = GMailService.DecodeToMessage(Body.Raw);
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
                var result = new ListMessagesResponse
                {
                    Messages = GmailUtil.GetFakeGmailMessageList()
                };

                return Task.FromResult(result);
            }
        }
    }
}
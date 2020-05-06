// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models.ServiceNow;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using RestSharp;

namespace ITSMSkill.Tests.API.Fakes
{
    public class MockServiceNowRestClient
    {
        private static readonly GetUserIdResponse GetUserIdResponse = new GetUserIdResponse
        {
            result = "MockUserId"
        };

        private static readonly SingleAggregateResponse CountTicketResponse = new SingleAggregateResponse
        {
            result = new AggregateResponse
            {
                stats = new StatsResponse
                {
                    count = MockData.TicketCount
                }
            }
        };

        private static readonly SingleTicketResponse CreateTicketResponse = new SingleTicketResponse
        {
            result = new TicketResponse
            {
                state = MockData.CreateTicketState,
                opened_at = MockData.CreateTicketOpenedTime,
                short_description = MockData.CreateTicketTitle,
                description = MockData.CreateTicketDescription,
                sys_id = MockData.CreateTicketId,
                urgency = MockData.CreateTicketUrgency,
                number = MockData.CreateTicketNumber
            }
        };

        private static readonly MultiTicketsResponse SearchTicketResponse = new MultiTicketsResponse
        {
            result = new List<TicketResponse>
            {
                new TicketResponse
                {
                    state = MockData.CreateTicketState,
                    opened_at = MockData.CreateTicketOpenedTime,
                    short_description = MockData.CreateTicketTitle,
                    description = MockData.CreateTicketDescription,
                    sys_id = MockData.CreateTicketId,
                    urgency = MockData.CreateTicketUrgency,
                    number = MockData.CreateTicketNumber
                }
            }
        };

        private static readonly MultiTicketsResponse SearchTicketToCloseResponse = new MultiTicketsResponse
        {
            result = new List<TicketResponse>
            {
                new TicketResponse
                {
                    state = MockData.CreateTicketState,
                    opened_at = MockData.CreateTicketOpenedTime,
                    short_description = MockData.CreateTicketTitle,
                    description = MockData.CreateTicketDescription,
                    sys_id = MockData.CloseTicketId,
                    urgency = MockData.CreateTicketUrgency,
                    number = MockData.CloseTicketNumber
                }
            }
        };

        private static readonly SingleTicketResponse CloseTicketResponse = new SingleTicketResponse
        {
            result = new TicketResponse
            {
                state = MockData.CloseTicketState,
                opened_at = MockData.CreateTicketOpenedTime,
                short_description = MockData.CreateTicketTitle,
                description = MockData.CreateTicketDescription,
                close_code = MockData.CloseTicketCloseCode,
                close_notes = MockData.CloseTicketReason,
                sys_id = MockData.CloseTicketId,
                urgency = MockData.CreateTicketUrgency,
                number = MockData.CloseTicketNumber
            }
        };

        private static readonly MultiKnowledgesResponse SearchKnowledgeResponse = new MultiKnowledgesResponse
        {
            result = new List<KnowledgeResponse>
            {
                new KnowledgeResponse
                {
                    short_description = MockData.KnowledgeTitle,
                    sys_updated_on = MockData.KnowledgeUpdatedTime,
                    text = MockData.KnowledgeContent,
                    number = MockData.KnowledgeNumber
                }
            }
        };

        private static readonly SingleAggregateResponse CountKnowledgeResponse = new SingleAggregateResponse
        {
            result = new AggregateResponse
            {
                stats = new StatsResponse
                {
                    count = MockData.TicketCount
                }
            }
        };

        public MockServiceNowRestClient()
        {
            var mockClient = new Mock<IRestClient>(MockBehavior.Strict);

            // TODO use Execute*TaskAsync instead of extension methods
            mockClient
               .Setup(c => c.ExecuteGetTaskAsync<GetUserIdResponse>(It.IsAny<IRestRequest>()))
               .ReturnsAsync(CreateGetUserIdResponseAndCount());

            mockClient
               .Setup(c => c.ExecuteGetTaskAsync<SingleAggregateResponse>(It.Is<IRestRequest>(r => r.Resource.StartsWith("now/v1/stats/incident") && r.Parameters.Any(p => p.Name == "sysparm_count" && p.Value is bool && (bool)p.Value))))
               .ReturnsAsync(CreateIRestResponse(CountTicketResponse));

            mockClient
               .Setup(c => c.ExecuteTaskAsync(It.Is<IRestRequest>(r => r.Resource.StartsWith("now/v1/table/incident")), It.IsIn(CancellationToken.None), It.IsIn(Method.POST)))
               .ReturnsAsync(new RestResponse { StatusCode = System.Net.HttpStatusCode.Created, Content = JsonConvert.SerializeObject(CreateTicketResponse) });

            mockClient
               .Setup(c => c.ExecuteGetTaskAsync<MultiTicketsResponse>(It.Is<IRestRequest>(r => r.Resource.StartsWith("now/v1/table/incident"))))
               .ReturnsAsync(CreateIRestResponse(SearchTicketResponse));

            // The last wins
            mockClient
               .Setup(c => c.ExecuteGetTaskAsync<MultiTicketsResponse>(It.Is<IRestRequest>(r => r.Resource.StartsWith("now/v1/table/incident") && IsTicketToClose(r))))
               .ReturnsAsync(CreateIRestResponse(SearchTicketToCloseResponse));

            // TODO use id is not an ideal way to distinguish
            mockClient
               .Setup(c => c.ExecuteTaskAsync(It.Is<IRestRequest>(r => r.Resource.StartsWith($"now/v1/table/incident/{MockData.CreateTicketId}")), It.IsIn(CancellationToken.None), It.IsIn(Method.PATCH)))
               .ReturnsAsync(new RestResponse { StatusCode = System.Net.HttpStatusCode.OK, Content = JsonConvert.SerializeObject(CreateTicketResponse) });

            mockClient
               .Setup(c => c.ExecuteTaskAsync(It.Is<IRestRequest>(r => r.Resource.StartsWith($"now/v1/table/incident/{MockData.CloseTicketId}")), It.IsIn(CancellationToken.None), It.IsIn(Method.PATCH)))
               .ReturnsAsync(new RestResponse { StatusCode = System.Net.HttpStatusCode.OK, Content = JsonConvert.SerializeObject(CloseTicketResponse) });

            mockClient
               .Setup(c => c.ExecuteGetTaskAsync<MultiKnowledgesResponse>(It.Is<IRestRequest>(r => r.Resource.StartsWith("now/v1/table/kb_knowledge"))))
               .ReturnsAsync(CreateIRestResponse(SearchKnowledgeResponse));

            mockClient
               .Setup(c => c.ExecuteGetTaskAsync<SingleAggregateResponse>(It.Is<IRestRequest>(r => r.Resource.StartsWith("now/v1/stats/kb_knowledge") && r.Parameters.Any(p => p.Name == "sysparm_count" && p.Value is bool && (bool)p.Value))))
               .ReturnsAsync(CreateIRestResponse(CountKnowledgeResponse));

            MockRestClient = mockClient.Object;
        }

        public IRestClient MockRestClient { get; }

        public int GetUserIdResponseCount { get; set; } = 0;

        private IRestResponse<GetUserIdResponse> CreateGetUserIdResponseAndCount()
        {
            GetUserIdResponseCount += 1;
            return CreateIRestResponse(GetUserIdResponse);
        }

        private IRestResponse<T> CreateIRestResponse<T>(T data)
        {
            var mockResponse = new Mock<IRestResponse<T>>(MockBehavior.Strict);
            mockResponse.Setup(r => r.Data).Returns(data);
            mockResponse.Setup(r => r.ResponseStatus).Returns(ResponseStatus.Completed);
            return mockResponse.Object;
        }

        private static bool IsTicketToClose(IRestRequest restRequest)
        {
            return restRequest.Parameters.Any(p => p.Name == "sysparm_query" && p.Value is string && ((string)p.Value).Contains(MockData.CloseTicketNumber));
        }
    }
}

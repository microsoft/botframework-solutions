using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.People.v1;
using Google.Apis.People.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Moq;
using static Google.Apis.People.v1.PeopleResource;

namespace CalendarSkillTest.API.Fakes.MockGoogleClient
{
    public static class MockGoogleUserClient
    {
        private static Mock<PeopleService> mockGoogleUserService;
        private static Mock<PeopleResource> mockPeopleResource;
        private static Mock<ConnectionsResource> mockConnectionsResource;

        static MockGoogleUserClient()
        {
            mockGoogleUserService = new Mock<PeopleService>();
            mockPeopleResource = new Mock<PeopleResource>(mockGoogleUserService.Object);
            mockConnectionsResource = new Mock<ConnectionsResource>(mockGoogleUserService.Object);
            mockGoogleUserService.SetupGet(service => service.People).Returns(mockPeopleResource.Object);
            mockPeopleResource.SetupGet(peopleResource => peopleResource.Connections).Returns(mockConnectionsResource.Object);
            mockConnectionsResource.Setup(connect => connect.List(It.IsAny<string>())).Returns((string resourceName) =>
            {
                if (resourceName != "people/me")
                {
                    throw new Exception("Resource Name not support");
                }

                MockListRequest mockListRequest = new MockListRequest(mockGoogleUserService.Object, resourceName);
                return mockListRequest;
            });
        }

        public static PeopleService GetPeopleService()
        {
            return mockGoogleUserService.Object;
        }

        private class MockListRequest : ConnectionsResource.ListRequest, IClientServiceRequest<ListConnectionsResponse>
        {
            public MockListRequest(IClientService service, string resourceName)
                : base(service, resourceName)
            {
            }

            public new async Task<ListConnectionsResponse> ExecuteAsync()
            {
                if (ResourceName != "people/me")
                {
                    throw new Exception("Resource Name not support");
                }

                ListConnectionsResponse result = new ListConnectionsResponse
                {
                    Connections = new List<Person>()
                };

                Person person = new Person()
                {
                    Names = new List<Name>(),
                    EmailAddresses = new List<EmailAddress>()
                };

                person.Names.Add(new Name() { DisplayName = "Jane Doe", GivenName = "Jane", FamilyName = "Doe", DisplayNameLastFirst = "Jane Doe" });
                person.EmailAddresses.Add(new EmailAddress() { Value = "JaneDeo@test.com" });
                result.Connections.Add(person);

                person = new Person()
                {
                    Names = new List<Name>(),
                    EmailAddresses = new List<EmailAddress>()
                };

                person.Names.Add(new Name() { DisplayName = "John Doe", GivenName = "John", FamilyName = "Doe", DisplayNameLastFirst = "John Doe" });
                person.EmailAddresses.Add(new EmailAddress() { Value = "JohnDeo@test.com" });
                result.Connections.Add(person);

                await Task.CompletedTask;
                return result;
            }
        }
    }
}

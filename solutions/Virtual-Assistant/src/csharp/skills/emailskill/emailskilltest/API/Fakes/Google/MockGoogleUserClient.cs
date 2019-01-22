using System;
using Google.Apis.People.v1;
using Moq;
using static Google.Apis.People.v1.PeopleResource;

namespace EmailSkillTest.API.Fakes.Google
{
    public static class MockGoogleUserClient
    {
        private static Mock<PeopleResource> mockPeopleResource;
        private static Mock<ConnectionsResource> mockConnectionsResource;

        static MockGoogleUserClient()
        {
            MockGoogleUserService = new Mock<PeopleService>();
            mockPeopleResource = new Mock<PeopleResource>(MockGoogleUserService.Object);
            mockConnectionsResource = new Mock<ConnectionsResource>(MockGoogleUserService.Object);
            MockGoogleUserService.SetupGet(service => service.People).Returns(mockPeopleResource.Object);
            mockPeopleResource.SetupGet(peopleResource => peopleResource.Connections).Returns(mockConnectionsResource.Object);
            mockConnectionsResource.Setup(connect => connect.List(It.IsAny<string>())).Returns((string resourceName) =>
            {
                if (resourceName != "people/me")
                {
                    throw new Exception("Resource Name not support");
                }

                MockListRequest mockListRequest = new MockListRequest(MockGoogleUserService.Object, resourceName);
                return mockListRequest;
            });
        }

        public static Mock<PeopleService> MockGoogleUserService { get; set; }
    }
}
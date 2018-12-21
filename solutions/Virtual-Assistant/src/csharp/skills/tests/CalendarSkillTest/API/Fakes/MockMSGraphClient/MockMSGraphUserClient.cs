using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using Moq;

namespace CalendarSkillTest.API.Fakes.MockMSGraphClient
{
    public static class MockMSGraphUserClient
    {
        private static Mock<IGraphServiceClient> mockMsGraphUserService;

        static MockMSGraphUserClient()
        {
            mockMsGraphUserService = new Mock<IGraphServiceClient>();
            mockMsGraphUserService.Setup(service => service.Users.Request(It.IsAny<List<QueryOption>>()).GetAsync()).Returns(() =>
            {
                IGraphServiceUsersCollectionPage result = new GraphServiceUsersCollectionPage();

                User user = new User()
                {
                    DisplayName = "Jane Doe",
                    GivenName = "Jane",
                    Surname = "Doe",
                    UserPrincipalName = "Jane Doe",
                    Mail = "JaneDoe@test.com"
                };
                result.Add(user);

                user = new User()
                {
                    DisplayName = "John Doe",
                    GivenName = "John",
                    Surname = "Doe",
                    UserPrincipalName = "John Doe",
                    Mail = "JohnDoe@test.com"
                };
                result.Add(user);

                user = new User()
                {
                    DisplayName = "Conf Room Test",
                    Mail = "ConfRoom@test.com"
                };
                result.Add(user);

                return Task.FromResult(result);
            });
            mockMsGraphUserService.Setup(service => service.Me.Contacts.Request(It.IsAny<List<QueryOption>>()).GetAsync()).Returns(() =>
            {
                IUserContactsCollectionPage result = new UserContactsCollectionPage();

                List<EmailAddress> emailAddresses = new List<EmailAddress>
                {
                    new EmailAddress() { Address = "JaneDoe@test.com" }
                };
                Contact contact = new Contact()
                {
                    DisplayName = "Jane Doe",
                    GivenName = "Jane",
                    Surname = "Doe",
                    EmailAddresses = emailAddresses
                };
                result.Add(contact);

                emailAddresses = new List<EmailAddress>
                {
                    new EmailAddress() { Address = "JohnDoe@test.com" }
                };
                contact = new Contact()
                {
                    DisplayName = "John Doe",
                    GivenName = "John",
                    Surname = "Doe",
                    EmailAddresses = emailAddresses
                };
                result.Add(contact);

                emailAddresses = new List<EmailAddress>
                {
                    new EmailAddress() { Address = "ConfRoom@test.com" }
                };
                contact = new Contact()
                {
                    DisplayName = "Conf Room Test",
                    EmailAddresses = emailAddresses
                };
                result.Add(contact);

                return Task.FromResult(result);
            });
            mockMsGraphUserService.Setup(service => service.Me.People.Request(It.IsAny<List<QueryOption>>()).GetAsync()).Returns(() =>
            {
                IUserPeopleCollectionPage result = new UserPeopleCollectionPage();

                List<ScoredEmailAddress> emailAddresses = new List<ScoredEmailAddress>
                {
                    new ScoredEmailAddress() { Address = "JaneDoe@test.com", RelevanceScore = 1 }
                };
                Person person = new Person()
                {
                    DisplayName = "Jane Doe",
                    GivenName = "Jane",
                    Surname = "Doe",
                    UserPrincipalName = "Jane Doe",
                    ScoredEmailAddresses = emailAddresses
                };
                result.Add(person);

                emailAddresses = new List<ScoredEmailAddress>
                {
                    new ScoredEmailAddress() { Address = "JohnDoe@test.com", RelevanceScore = 1 }
                };
                person = new Person()
                {
                    DisplayName = "John Doe",
                    GivenName = "John",
                    Surname = "Doe",
                    UserPrincipalName = "John Doe",
                    ScoredEmailAddresses = emailAddresses
                };
                result.Add(person);

                emailAddresses = new List<ScoredEmailAddress>
                {
                    new ScoredEmailAddress() { Address = "ConfRoom@test.com", RelevanceScore = 1 }
                };
                person = new Person()
                {
                    DisplayName = "Conf Room Test",
                    ScoredEmailAddresses = emailAddresses
                };
                result.Add(person);

                return Task.FromResult(result);
            });
        }

        public static IGraphServiceClient GetUserService()
        {
            return mockMsGraphUserService.Object;
        }
    }
}

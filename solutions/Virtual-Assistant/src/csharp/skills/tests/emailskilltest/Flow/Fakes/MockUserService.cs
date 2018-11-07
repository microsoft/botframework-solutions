using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmailSkill;
using Microsoft.Graph;

namespace EmailSkillTest.Flow.Fakes
{
    public class MockUserService : IUserService
    {
        public MockUserService()
        {
            this.Users = FakeUsers();
            this.People = FakePeople();
            this.Contacts = FakeContacts();
        }

        public List<Person> People { get; set; }

        public List<User> Users { get; set; }

        public List<Contact> Contacts { get; set; }

        public async Task<List<Person>> GetPeopleAsync(string name)
        {
            return this.People;
        }

        public async Task<List<User>> GetUserAsync(string name)
        {
            return this.Users;
        }

        public async Task<List<Contact>> GetContactsAsync(string name)
        {
            return this.Contacts;
        }

        private List<Person> FakePeople()
        {
            var people = new List<Person>();

            var addressList = new List<ScoredEmailAddress>();
            var emailAddress = new ScoredEmailAddress()
            {
                Address = "test@test.com",
                RelevanceScore = 1,
            };
            addressList.Add(emailAddress);

            people.Add(new Person()
            {
                UserPrincipalName = "test@test.com",
                ScoredEmailAddresses = addressList,
                DisplayName = "Test Test",
            });

            return people;
        }

        private List<User> FakeUsers()
        {
            var users = new List<User>();

            var emailAddressStr = "test@test.com";
            users.Add(new User()
            {
                UserPrincipalName = "test@test.com",
                Mail = emailAddressStr,
                DisplayName = "Test Test",
            });

            return users;
        }

        private List<Contact> FakeContacts()
        {
            var contacts = new List<Contact>();

            var addressList = new List<EmailAddress>();
            var emailAddress = new EmailAddress()
            {
                Address = "test@test.com",
            };
            addressList.Add(emailAddress);

            contacts.Add(new Contact()
            {
                EmailAddresses = addressList,
                DisplayName = "Test Test",
            });

            return contacts;
        }
    }
}

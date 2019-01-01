using System.Collections.Generic;
using System.Threading.Tasks;
using EmailSkill.ServiceClients;
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

        public Task<List<Person>> GetPeopleAsync(string name)
        {
            var result = new List<Person>();

            foreach (var person in this.People)
            {
                if (person.DisplayName == name)
                {
                    result.Add(person);
                }
            }

            return Task.FromResult(result);
        }

        public Task<List<User>> GetUserAsync(string name)
        {
            var result = new List<User>();

            foreach (var user in this.Users)
            {
                if (user.DisplayName == name)
                {
                    result.Add(user);
                }
            }

            return Task.FromResult(result);
        }

        public Task<List<Contact>> GetContactsAsync(string name)
        {
            var result = new List<Contact>();

            foreach (var contact in this.Contacts)
            {
                if (contact.DisplayName == name)
                {
                    result.Add(contact);
                }
            }

            return Task.FromResult(result);
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

        private List<User> FakeUsers(int dupSize = 5)
        {
            var users = new List<User>();

            var emailAddressStr = "test@test.com";
            users.Add(new User()
            {
                UserPrincipalName = emailAddressStr,
                Mail = emailAddressStr,
                DisplayName = "Test Test",
            });

            for (int i = 0; i < dupSize; i++)
            {
                emailAddressStr = "testdup" + i + "@test.com";
                users.Add(new User()
                {
                    UserPrincipalName = emailAddressStr,
                    Mail = emailAddressStr,
                    DisplayName = "TestDup Test",
                });
            }

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
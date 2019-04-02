using System.Collections.Generic;
using System.Threading.Tasks;
using EmailSkill.Model;
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

        public List<PersonModel> People { get; set; }

        public List<PersonModel> Users { get; set; }

        public List<PersonModel> Contacts { get; set; }

        public Task<List<PersonModel>> GetPeopleAsync(string name)
        {
            var result = new List<PersonModel>();

            foreach (var person in this.People)
            {
                if (person.DisplayName == name)
                {
                    result.Add(person);
                }
            }

            return Task.FromResult(result);
        }

        public Task<List<PersonModel>> GetUserAsync(string name)
        {
            var result = new List<PersonModel>();

            foreach (var user in this.Users)
            {
                if (user.DisplayName == name)
                {
                    result.Add(user);
                }
            }

            return Task.FromResult(result);
        }

        public Task<List<PersonModel>> GetContactsAsync(string name)
        {
            var result = new List<PersonModel>();

            foreach (var contact in this.Contacts)
            {
                if (contact.DisplayName == name)
                {
                    result.Add(contact);
                }
            }

            return Task.FromResult(result);
        }

        private List<PersonModel> FakePeople()
        {
            var people = new List<PersonModel>();

            var emailAddressStr = "test@test.com";
            var mails = new List<string>();
            mails.Add(emailAddressStr);

            people.Add(new PersonModel()
            {
                UserPrincipalName = "test@test.com",
                Emails = mails,
                DisplayName = "Test Test",
            });

            return people;
        }

        private List<PersonModel> FakeUsers(int dupSize = 5)
        {
            var users = new List<PersonModel>();

            var emailAddressStr = "test@test.com";
            var mails = new List<string>();
            mails.Add(emailAddressStr);
            users.Add(new PersonModel()
            {
                UserPrincipalName = emailAddressStr,
                Emails = mails,
                DisplayName = "Test Test",
            });

            for (int i = 0; i < dupSize; i++)
            {
                emailAddressStr = "testdup" + i + "@test.com";
                var emails = new List<string>();
                emails.Add(emailAddressStr);
                users.Add(new PersonModel()
                {
                    UserPrincipalName = emailAddressStr,
                    Emails = emails,
                    DisplayName = "TestDup Test",
                });
            }

            return users;
        }

        private List<PersonModel> FakeContacts()
        {
            var contacts = new List<PersonModel>();

            var addressList = new List<string>();
            addressList.Add("test@test.com");

            contacts.Add(new PersonModel()
            {
                Emails = addressList,
                DisplayName = "Test Test",
            });

            return contacts;
        }

        public Task<PersonModel> GetMeAsync()
        {
            var addressList = new List<string>();
            addressList.Add("test@test.com");
            var user = new PersonModel()
            {
                UserPrincipalName = "Test Test",
                Emails = addressList,
                DisplayName = "Test Test",
            };
            return Task.FromResult(user);
        }

        public Task<string> GetPhotoAsync(string id)
        {
            return Task.FromResult("data:image/gif;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQImWNgYGBgAAAABQABh6FO1AAAAABJRU5ErkJggg==");
        }
    }
}
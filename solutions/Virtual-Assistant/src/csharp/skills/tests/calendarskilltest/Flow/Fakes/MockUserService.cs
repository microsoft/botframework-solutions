using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CalendarSkill;
using CalendarSkill.Extensions;
using Microsoft.Graph;

namespace CalendarSkillTest.Flow.Fakes
{
    public class MockUserService : IUserService
    {
        public MockUserService(List<User> fakeUsers, List<Person> fakePeople)
        {
            this.Users = fakeUsers ?? new List<User>();
            this.People = fakePeople ?? new List<Person>();
        }

        public List<Person> People { get; set; }

        public List<User> Users { get; set; }

        public async Task<List<PersonModel>> GetContactsAsync(string name)
        {
            List<PersonModel> items = new List<PersonModel>();
            return await Task.FromResult(items);
        }

        public async Task<List<PersonModel>> GetPeopleAsync(string name)
        {
            List<PersonModel> items = new List<PersonModel>();
            foreach (Person people in this.People)
            {
                items.Add(new PersonModel(people));
            }

            return await Task.FromResult(items);
        }

        public async Task<List<PersonModel>> GetUserAsync(string name)
        {
            List<PersonModel> items = new List<PersonModel>();
            foreach (User user in this.Users)
            {
                items.Add(new PersonModel(user.ToPerson()));
            }

            return await Task.FromResult(items);
        }

        public static List<Person> FakeDefaultPeople()
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

        public static List<User> FakeDefaultUsers()
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
    }
}

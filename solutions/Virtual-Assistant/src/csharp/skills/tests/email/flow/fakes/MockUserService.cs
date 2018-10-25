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
        }

        public List<Person> People { get; set; }

        public List<User> Users { get; set; }

        public async Task<List<Person>> GetPeopleAsync(string name)
        {
            return this.People;
        }

        public async Task<List<User>> GetUserAsync(string name)
        {
            return this.Users;
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

        //protected override async Task<IGraphServiceUsersCollectionPage> GetUserFromGraphAsync(List<QueryOption> optionList)
        //{
        //    IGraphServiceUsersCollectionPage result = new GraphServiceUsersCollectionPage();

        //    var emailAddress = "test@test.com";
        //    result.Add(new User()
        //    {
        //        UserPrincipalName = "test@test.com",
        //        Mail = emailAddress,
        //        DisplayName = "Test Test",
        //    });

        //    await Task.CompletedTask;
        //    return result;
        //}

        //protected override async Task<IUserPeopleCollectionPage> GetPeopleFromGraphAsync(List<QueryOption> optionList)
        //{
        //    IUserPeopleCollectionPage result = new UserPeopleCollectionPage();

        //    var addressList = new List<ScoredEmailAddress>();
        //    var emailAddress = new ScoredEmailAddress()
        //    {
        //        Address = "test@test.com",
        //        RelevanceScore = 1,
        //    };
        //    addressList.Add(emailAddress);
        //    result.Add(new Person()
        //    {
        //        UserPrincipalName = "test@test.com",
        //        ScoredEmailAddresses = addressList,
        //        DisplayName = "Test Test",
        //    });

        //    await Task.CompletedTask;
        //    return result;
        //}
    }
}

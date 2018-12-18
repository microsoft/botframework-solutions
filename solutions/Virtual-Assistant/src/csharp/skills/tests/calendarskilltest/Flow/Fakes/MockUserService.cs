using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Extensions;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using Microsoft.Bot.Solutions.Skills;
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

        public static List<User> FakeDefaultUsers()
        {
            var users = new List<User>();

            var emailAddressStr = Strings.Strings.DefaultUserEmail;
            users.Add(new User()
            {
                UserPrincipalName = Strings.Strings.DefaultUserEmail,
                Mail = emailAddressStr,
                DisplayName = Strings.Strings.DefaultUserName,
            });

            return users;
        }

        public static List<User> FakeMultipleUsers(int count)
        {
            var users = new List<User>();

            for (int i = 0; i < count; i++)
            {
                var emailAddressStr = string.Format(Strings.Strings.UserEmailAddress, i);
                var userNameStr = string.Format(Strings.Strings.UserName, i);
                users.Add(new User()
                {
                    UserPrincipalName = userNameStr,
                    Mail = emailAddressStr,
                    DisplayName = userNameStr,
                });
            }

            return users;
        }

        public static List<Person> FakeMultiplePeoples(int count)
        {
            var people = new List<Person>();

            for (int i = 0; i < count; i++)
            {
                var emailAddressStr = string.Format(Strings.Strings.UserEmailAddress, i);
                var userNameStr = string.Format(Strings.Strings.UserName, i);
                var addressList = new List<ScoredEmailAddress>();
                var emailAddress = new ScoredEmailAddress()
                {
                    Address = emailAddressStr,
                    RelevanceScore = 1,
                };
                addressList.Add(emailAddress);

                people.Add(new Person()
                {
                    UserPrincipalName = emailAddressStr,
                    ScoredEmailAddresses = addressList,
                    DisplayName = userNameStr,
                });
            }

            return people;
        }

        public static List<Person> FakeDefaultPeople()
        {
            var people = new List<Person>();

            var addressList = new List<ScoredEmailAddress>();
            var emailAddress = new ScoredEmailAddress()
            {
                Address = Strings.Strings.DefaultUserEmail,
                RelevanceScore = 1,
            };
            addressList.Add(emailAddress);

            people.Add(new Person()
            {
                UserPrincipalName = Strings.Strings.DefaultUserEmail,
                ScoredEmailAddresses = addressList,
                DisplayName = Strings.Strings.DefaultUserName,
            });

            return people;
        }

        public async Task<List<PersonModel>> GetContactsAsync(string name)
        {
            List<PersonModel> items = new List<PersonModel>();
            return await Task.FromResult(items);
        }

        public async Task<List<PersonModel>> GetPeopleAsync(string name)
        {
            if (name == Strings.Strings.ThrowErrorAccessDenied)
            {
                throw new SkillException(SkillExceptionType.APIAccessDenied, Strings.Strings.ThrowErrorAccessDenied, new Exception());
            }

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
    }
}

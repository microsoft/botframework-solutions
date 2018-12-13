﻿using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CalendarSkill;
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

        public async Task<List<Contact>> GetContactsAsync(string name)
        {
            List<Contact> items = new List<Contact>();
            return await Task.FromResult(items);
        }

        public async Task<List<Person>> GetPeopleAsync(string name)
        {
            return await Task.FromResult(this.People);
        }

        public async Task<List<User>> GetUserAsync(string name)
        {
            return await Task.FromResult(this.Users);
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

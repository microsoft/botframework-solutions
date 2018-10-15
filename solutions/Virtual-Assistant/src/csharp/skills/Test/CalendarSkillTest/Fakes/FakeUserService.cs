// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkillTest.Fakes
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CalendarSkill;
    using Microsoft.Graph;

    public class FakeUserService : IUserService
    {
        private readonly string token;

        public FakeUserService(string token)
        {
            this.token = token;
        }

        public async Task<List<Person>> GetPeopleAsync(string name)
        {
            var result = new List<Person>();
            if (name == "TestName")
            {
                var addressList = new List<ScoredEmailAddress>();
                var emailAddress = new ScoredEmailAddress()
                {
                    Address = "test@test.com",
                    RelevanceScore = 1,
                };
                addressList.Add(emailAddress);
                result.Add(new Person()
                {
                    UserPrincipalName = "test@test.com",
                    ScoredEmailAddresses = addressList,
                    DisplayName = "TestName",
                });
            }

            return result;
        }

        public async Task<List<User>> GetUserAsync(string name)
        {
            var result = new List<User>();
            if (name == "test")
            {
                var emailAddress = "test@test.com";
                result.Add(new User()
                {
                    UserPrincipalName = "test@test.com",
                    Mail = emailAddress,
                    DisplayName = "TestName",
                });
            }

            return result;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Graph;

    public static class UserEx
    {
        /// <summary>
        /// Convert User to Person.
        /// </summary>
        /// <param name="user">User Instance.</param>
        /// <returns>Person Instance.</returns>
        public static Person ToPerson(this User user)
        {
            var person = new Person
            {
                DisplayName = user.DisplayName,
                UserPrincipalName = user.UserPrincipalName,
                Surname = user.Surname,
            };
            var emailAddresses = new List<ScoredEmailAddress>
            {
                new ScoredEmailAddress()
                {
                    Address = user.Mail,
                    RelevanceScore = 1.0,
                },
            };
            person.ScoredEmailAddresses = emailAddresses;
            person.ImAddress = user.ImAddresses?.FirstOrDefault();
            person.JobTitle = user.JobTitle;
            return person;
        }
    }
}

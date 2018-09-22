// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace CalendarSkill
{
    public interface IUserService
    {
        Task<List<Person>> GetPeopleAsync(string name);

        Task<List<User>> GetUserAsync(string name);
    }
}

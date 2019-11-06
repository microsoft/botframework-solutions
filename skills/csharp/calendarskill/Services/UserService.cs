// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;

namespace CalendarSkill.Services
{
    public class UserService : IUserService
    {
        private IUserService userService;

        public UserService(IUserService userService)
        {
            if (userService == null)
            {
                throw new Exception("userService is null");
            }

            this.userService = userService;
        }

        public async Task<List<PersonModel>> GetPeopleAsync(string name)
        {
            return await userService.GetPeopleAsync(name);
        }

        public async Task<List<PersonModel>> GetUserAsync(string name)
        {
            return await userService.GetUserAsync(name);
        }

        public async Task<List<PersonModel>> GetContactsAsync(string name)
        {
            return await userService.GetContactsAsync(name);
        }

        public async Task<PersonModel> GetMeAsync()
        {
            return await userService.GetMeAsync();
        }

        public async Task<PersonModel> GetMyManagerAsync()
        {
            return await userService.GetMyManagerAsync();
        }

        public async Task<PersonModel> GetManagerAsync(string name)
        {
            return await userService.GetManagerAsync(name);
        }

        public async Task<string> GetPhotoAsync(string id)
        {
            return await userService.GetPhotoAsync(id);
        }
    }
}

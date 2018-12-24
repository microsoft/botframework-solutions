// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ToDoSkill;
using ToDoSkillTest.Flow.Fakes;

namespace ToDoSkillTest.Fakes
{
    public class MockMailService : IMailService
    {
        public MockMailService()
        {
        }

        public Task<IMailService> InitAsync(string token)
        {
            return Task.FromResult(this as IMailService);
        }

        public Task SendMessageAsync(string content, string subject)
        {
            return Task.CompletedTask;
        }

        public Task<string> GetSenderMailAddressAsync()
        {
            return Task.FromResult("test@outlook.com");
        }
    }
}
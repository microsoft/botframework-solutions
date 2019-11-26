// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using ToDoSkill.Models;
using ToDoSkill.Services;
using ToDoSkill.Tests.Fakes;

namespace ToDoSkill.Tests.Flow.Fakes
{
    public class MockServiceManager : IServiceManager
    {
        public MockServiceManager()
        {
            MockTaskService = new MockTaskService();
        }

        public MockTaskService MockTaskService { get; set; }

        public IMailService InitMailService(string token)
        {
            var service = new MockMailService();
            return service.InitAsync(token).Result;
        }

        public ITaskService InitTaskService(string token, Dictionary<string, string> listTypeIds, ServiceProviderType taskServiceType)
        {
            return MockTaskService.InitAsync(token, listTypeIds).Result;
        }
    }
}
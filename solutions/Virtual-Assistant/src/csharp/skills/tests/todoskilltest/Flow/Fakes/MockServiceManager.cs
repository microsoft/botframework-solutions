using System.Collections.Generic;
using ToDoSkill.Dialogs.Shared;
using ToDoSkill.ServiceClients;
using ToDoSkillTest.Fakes;

namespace ToDoSkillTest.Flow.Fakes
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

        public ITaskService InitTaskService(string token, Dictionary<string, string> listTypeIds, ServiceProviderTypes.ProviderTypes taskServiceType)
        {
            return MockTaskService.InitAsync(token, listTypeIds).Result;
        }
    }
}
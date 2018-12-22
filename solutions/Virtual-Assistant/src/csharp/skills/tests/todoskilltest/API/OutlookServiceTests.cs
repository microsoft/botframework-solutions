using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill;
using ToDoSkillTest.API.Fakes;

namespace ToDoSkillTest.API
{
    [TestClass]
    public class OutlookServiceTests
    {
        private HttpClient mockClient;

        [TestInitialize]
        public void Initialize()
        {
            mockClient = new HttpClient(new MockHttpClientHandlerGen().GetMockHttpClientHandler());
        }

        [TestMethod]
        public async Task AddTaskTests()
        {
            var service = new OutlookService();
            var pageId = new Dictionary<string, string>() { { "ToDo", "ToDo" }, { "Grocery", "Grocery" }, { "Shopping", "Shopping" } };

            await service.InitAsync("test", pageId, mockClient);

            var taskList = await service.GetTasksAsync("ToDo");
            await service.AddTaskAsync("ToDo", "Test 9");
            var addedTaskList = await service.GetTasksAsync("ToDo");

            Assert.IsTrue(taskList.Count + 1 == addedTaskList.Count);
        }

        [TestMethod]
        public async Task MarkTaskTest()
        {
            var service = new OutlookService();
            var pageId = new Dictionary<string, string>() { { "ToDo", "ToDo" }, { "Grocery", "Grocery" }, { "Shopping", "Shopping" } };

            await service.InitAsync("test", pageId, mockClient);

            var taskList = await service.GetTasksAsync("ToDo");
            await service.MarkTasksCompletedAsync("ToDo", taskList.GetRange(0, 1));
            var markedTaskList = await service.GetTasksAsync("ToDo");

            Assert.IsTrue(markedTaskList != null && markedTaskList.Count > 0 && markedTaskList[0].IsCompleted);
        }

        [TestMethod]
        public async Task DeleteTaskTest()
        {
            var service = new OutlookService();
            var pageId = new Dictionary<string, string>() { { "ToDo", "ToDo" }, { "Grocery", "Grocery" }, { "Shopping", "Shopping" } };

            await service.InitAsync("test", pageId, mockClient);

            var taskList = await service.GetTasksAsync("ToDo");
            await service.DeleteTasksAsync("ToDo", taskList.GetRange(0, 1));
            var deletedTaskList = await service.GetTasksAsync("ToDo");

            Assert.IsTrue(taskList.Count == deletedTaskList.Count + 1);
            Assert.IsFalse(deletedTaskList.Contains(taskList[0]));
        }

        [TestMethod]
        public async Task ShowTaskTest()
        {
            var service = new OutlookService();
            var pageId = new Dictionary<string, string>() { { "ToDo", "ToDo" }, { "Grocery", "Grocery" }, { "Shopping", "Shopping" } };

            await service.InitAsync("test", pageId, mockClient);
            var taskList = await service.GetTasksAsync("ToDo");

            Assert.IsTrue(taskList != null && taskList.Count > 0);
        }

        [TestMethod]
        public async Task AddTaskAccessDeniedTests()
        {
            var service = new OutlookService();
            var pageId = new Dictionary<string, string>() { { "ToDo", "ToDo" }, { "Grocery", "Grocery" }, { "Shopping", "Shopping" } };
            try
            {
                await service.InitAsync("test", pageId, mockClient);
                await service.AddTaskAsync("Shopping", "Test 9");
                Assert.Fail();
            }
            catch (SkillException ex)
            {
                Assert.AreEqual(ex.ExceptionType, SkillExceptionType.APIAccessDenied);
            }
        }
    }
}

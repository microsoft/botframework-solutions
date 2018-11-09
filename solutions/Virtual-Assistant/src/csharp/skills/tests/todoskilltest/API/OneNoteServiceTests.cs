using System;
using System.Net.Http;
using System.Threading.Tasks;
using ToDoSkillTest.API.Fakes;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill;
using System.Collections.Generic;

namespace ToDoSkillTest.API
{
    [TestClass]
    public class OneNoteServiceTests
    {
        static private HttpClient mockClient;
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            mockClient = new HttpClient(MockHttpClient.getMockHttpClient());
        }

        [TestMethod]
        public async Task AddTaskTests()
        {
            var service = new OneNoteService();
            var pageId = new Dictionary<string, string>() { { "ToDo", "ToDo" }, { "Grocery", "Grocery" }, {"Shopping", "Shopping"} };

            await service.InitAsync("test", pageId, mockClient);

            var taskList = await service.GetTasksAsync("ToDo");
            await service.AddTaskAsync("ToDo", "Test 9");
            var addedTaskList = await service.GetTasksAsync("ToDo");

            Assert.IsTrue(taskList.Count + 1 == addedTaskList.Count);

        }

        [TestMethod]
        public async Task MarkTaskTest()
        {
            var service = new OneNoteService();
            var pageId = new Dictionary<string, string>() { { "ToDo", "ToDo" }, { "Grocery", "Grocery" }, { "Shopping", "Shopping" } };

            await service.InitAsync("test", pageId, mockClient);

            var taskList = await service.GetTasksAsync("ToDo");
            await service.MarkTasksCompletedAsync("ToDo", taskList.GetRange(0, 1));
            var markedTaskList = await service.GetTasksAsync("ToDo");

            Assert.IsTrue(markedTaskList[0].IsCompleted);
        }

        [TestMethod]
        public async Task DeleteTaskTest()
        {
            var service = new OneNoteService();
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
            var service = new OneNoteService();
            var pageId = new Dictionary<string, string>() { { "ToDo", "ToDo" }, { "Grocery", "Grocery" }, { "Shopping", "Shopping" } };

            await service.InitAsync("test", pageId, mockClient);
            var taskList = await service.GetTasksAsync("ToDo");

            Assert.IsTrue(taskList != null && taskList.Count > 0);
        }
    }
}

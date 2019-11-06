﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Services;
using ToDoSkill.Tests.API.Fakes;

namespace ToDoSkill.Tests.API
{
    [TestClass]
    public class OneNoteServiceTests
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
            var service = new OneNoteService();
            var pageId = new Dictionary<string, string>();

            await service.InitAsync(MockData.Token, pageId, mockClient);

            var taskList = await service.GetTasksAsync(MockData.ToDo);
            await service.AddTaskAsync(MockData.ToDo, MockData.TaskContent);
            var addedTaskList = await service.GetTasksAsync(MockData.ToDo);

            Assert.IsTrue(taskList.Count + 1 == addedTaskList.Count);
        }

        [TestMethod]
        public async Task MarkTaskTest()
        {
            var service = new OneNoteService();
            var pageId = new Dictionary<string, string>();

            await service.InitAsync(MockData.Token, pageId, mockClient);

            var taskList = await service.GetTasksAsync(MockData.ToDo);
            await service.MarkTasksCompletedAsync(MockData.ToDo, taskList.GetRange(0, 1));
            var markedTaskList = await service.GetTasksAsync(MockData.ToDo);

            Assert.IsTrue(markedTaskList != null && markedTaskList.Count == taskList.Count - 1 && markedTaskList[0].Id != taskList[0].Id);
        }

        [TestMethod]
        public async Task DeleteTaskTest()
        {
            var service = new OneNoteService();
            var pageId = new Dictionary<string, string>();

            await service.InitAsync(MockData.Token, pageId, mockClient);

            var taskList = await service.GetTasksAsync(MockData.ToDo);
            await service.DeleteTasksAsync(MockData.ToDo, taskList.GetRange(0, 1));
            var deletedTaskList = await service.GetTasksAsync(MockData.ToDo);

            Assert.IsTrue(taskList.Count == deletedTaskList.Count + 1);
            Assert.IsFalse(deletedTaskList.Contains(taskList[0]));
        }

        [TestMethod]
        public async Task ShowTaskTest()
        {
            var service = new OneNoteService();
            var pageId = new Dictionary<string, string>();

            await service.InitAsync(MockData.Token, pageId, mockClient);
            var taskList = await service.GetTasksAsync(MockData.ToDo);

            Assert.IsTrue(taskList != null && taskList.Count > 0);
            taskList.ForEach(t => Assert.AreEqual(false, t.IsCompleted));
        }
    }
}
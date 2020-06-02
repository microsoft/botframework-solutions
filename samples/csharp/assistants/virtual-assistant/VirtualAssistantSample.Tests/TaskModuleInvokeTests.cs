using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAssistantSample.Tests
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class TaskModuleInvokeTests : BotTestBase
    {
        [TestMethod]
        public async Task TestTaskModuleFetch()
        {
            // TaskModule Activity For Fetch
            var taskFetch = "{\r\n  \"data\": {\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"CreateTicket_Form\",\r\n   \"AppId\": \"TestSkill\",\r\n   \"Submit\": false\r\n    },\r\n    \"type\": \"task / fetch\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "task/fetch",
                Value = JObject.Parse(taskFetch)
            };

            await GetTestFlow()
                .Send(activity)
                .StartTestAsync();
        }
    }
}

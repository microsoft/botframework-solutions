using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VirtualAssistantSample.Bots;
using VirtualAssistantSample.Tests.Mocks;

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
            var sp = Services.BuildServiceProvider();
            var iLogger = sp.GetService<ILogger<DefaultActivityHandler<MockMainDialog>>>();
            var mockMainDialog = sp.GetService<MockMainDialog>();
            var defaultActivityHandler = new DefaultActivityHandler<MockMainDialog>(sp, iLogger, mockMainDialog);
            var adapter = sp.GetService<TestAdapter>();
            var turnContext = new TurnContext(adapter, activity);
            var taskModuleRequest = new TaskModuleRequest { Context = new TaskModuleRequestContext(), Data = taskFetch };
            //var handler = defaultActivityHandler.OnTeamsTaskModuleFetch(turnContext as ITurnContext<IInvokeActivity>, taskModuleRequest, CancellationToken.None);
            await GetTestFlow()
                .Send(activity)
                .StartTestAsync();
        }
    }
}

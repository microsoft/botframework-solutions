using System.Net;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            var taskFetch = "{\r\n  \"data\": {\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"CreateTicket_Form\",\r\n   \"SkillId\": \"TestSkill\",\r\n   \"Submit\": false\r\n    },\r\n    \"type\": \"task / fetch\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "task/fetch",
                Value = JObject.Parse(taskFetch)
            };
            await GetTestFlow()
                .Send(activity)
                .AssertReply(activity =>
                {
                    var result = activity as IInvokeActivity;

                    // Assert there is a card in the message
                    Assert.AreEqual("invokeResponse", activity.Type);
                    var val = (InvokeResponse)result.Value;
                    Assert.AreEqual((int)HttpStatusCode.OK, val.Status);
                    Assert.IsNotNull(val.Body);
                    var body = (TaskModuleResponse)val.Body;
                    var contiueResponse = (TaskModuleContinueResponse)body.Task;
                    Assert.AreEqual("FetchTaskModule", contiueResponse.Value.Title);
                })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task TestTaskModuleSubmit()
        {
            // TaskModule Activity For Fetch
            var taskSubmit = "{\r\n  \"data\": {\r\n    \"msteams\": {\r\n      \"type\": \"task/fetch\"\r\n    },\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"CreateTicket_Form\",\r\n   \"SkillId\": \"TestSkill\",\r\n    \"Submit\": true\r\n    },\r\n    \"IncidentTitle\": \"Test15\",\r\n    \"IncidentDescription\": \"Test15\",\r\n    \"IncidentUrgency\": \"Medium\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";

            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "task/fetch",
                Value = JObject.Parse(taskSubmit)
            };
            await GetTestFlow(isTaskSubmit: true)
                .Send(activity)
                .AssertReply(activity =>
                {
                    var result = activity as IInvokeActivity;

                    // Assert there is a card in the message
                    Assert.AreEqual("invokeResponse", activity.Type);
                    var val = (InvokeResponse)result.Value;
                    Assert.AreEqual((int)HttpStatusCode.OK, val.Status);
                    Assert.IsNotNull(val.Body);
                    var body = (TaskModuleResponse)val.Body;
                    var contiueResponse = (TaskModuleContinueResponse)body.Task;
                    Assert.AreEqual("SubmitTaskModule", contiueResponse.Value.Title);
                })
                .StartTestAsync();
        }
    }
}

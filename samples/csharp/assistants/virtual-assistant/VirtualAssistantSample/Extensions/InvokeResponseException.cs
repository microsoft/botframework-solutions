using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualAssistantSample.Extensions
{
    public static class InvokeResponseException
    {
        // Converts "InvokeResponse" sent by SkillHttpClient to "TaskModuleResponse"
        public static TaskModuleResponse GetTaskModuleRespose(this InvokeResponse invokeResponse)
        {
            if (invokeResponse.Body != null)
            {
                return new TaskModuleResponse()
                {
                    Task = GetTask(invokeResponse.Body),
                };
            }

            return null;
        }

        private static TaskModuleResponseBase GetTask(object invokeResponseBody)
        {
            var resposeBody = (JObject)JToken.FromObject(invokeResponseBody);
            var task = resposeBody.GetValue("task");
            string taskType = task.SelectToken("type").ToString();

            return taskType switch
            {
                "continue" => new TaskModuleContinueResponse()
                {
                    Type = taskType,
                    Value = task.SelectToken("value").ToObject<TaskModuleTaskInfo>(),
                },
                "message" => new TaskModuleMessageResponse()
                {
                    Type = taskType,
                    Value = task.SelectToken("value").ToString(),
                },
                _ => null,
            };
        }
    }
}

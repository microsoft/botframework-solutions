// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema.Teams;
using Newtonsoft.Json.Linq;

namespace VirtualAssistantSample.Extensions
{
    /// <summary>
    /// InvokeResposneHandler class for returning TaskModuleResponse from InvokeResponse
    /// </summary>
    public static class InvokeResponseExtensions
    {
        // Converts "InvokeResponse" sent by SkillHttpClient to "TaskModuleResponse"
        public static TaskModuleResponse GetTaskModuleResponse(this InvokeResponse invokeResponse)
        {
            if (invokeResponse == null)
            {
                throw new ArgumentNullException(nameof(invokeResponse));
            }

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
            var responseBody = JObject.FromObject(invokeResponseBody);
            var task = responseBody.GetValue("task");
            string taskType = task.SelectToken("type")?.Value<string>();

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
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using AdaptiveExpressions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VirtualAssistantSample.Models;

namespace VirtualAssistantSample.Extensions
{
    /// <summary>
    /// Extension class for getting SkillId from Activity.
    /// </summary>
    public static class InvokeActivityExtensions
    {
        // Fetches skillId from CardAction data if present
        public static string GetSkillId(this IInvokeActivity activity, ILogger logger)
        {
            if (activity == null)
            {
                logger.Log(LogLevel.Error, "activity is null from TaskModule");
                throw new ArgumentNullException(nameof(activity));
            }

            if (activity.Value == null)
            {
                logger.Log(LogLevel.Error, "activity.Value is null from TaskModule");
                throw new ArgumentException("activity.Value is null.", nameof(activity));
            }

            // GetSkillId from Activity Value
            var data = JObject.Parse(activity.Value.ToString()).SelectToken("data.data")?.ToObject<SkillCardActionData>();
            return data.SkillId ?? throw new ArgumentException("SkillId in TaskModule is null", nameof(SkillCardActionData));
        }
    }
}

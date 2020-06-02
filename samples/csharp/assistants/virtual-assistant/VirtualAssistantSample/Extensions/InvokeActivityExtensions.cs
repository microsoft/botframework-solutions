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
    /// Extension class for getting SkillId from Activity
    /// </summary>
    public static class InvokeActivityExtensions
    {
        // Fetches skillId from CardAction data if present
        public static string GetSkillId(this IInvokeActivity activity, ILogger logger)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<SkillCardActionData>(JObject.Parse(activity.Value.ToString()).SelectToken("data").SelectToken("data").ToString());
                return data.SkillId ?? throw new NullReferenceException("SkillId cannot be null");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Exception caught on attempting to Get SkillId : {ex}");

                throw ex;
            }
        }
    }
}

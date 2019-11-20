// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Schema;

namespace ToDoSkill.Utilities
{
    public class ToDoCommonUtil
    {
        public const int DefaultDisplaySize = 4;

        public static async Task<Activity> GetToDoResponseActivity(string templateName, ITurnContext turnContext, object data)
        {
            string formatTemplateName = "@{" + templateName + "()}";
            return await new ActivityTemplate(formatTemplateName).BindToData(turnContext, data);
        }
    }
}

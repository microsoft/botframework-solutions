// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.TemplateManager;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace $safeprojectname$.Extensions
{
    public class TemplateManagerWithVoice : TemplateManager
    {

        /// <summary>
        /// Send a reply with the template
        /// </summary>
        /// <param name="turnContext"></param>
        /// <param name="templateId"></param>
        /// <param name="data"></param>
        /// <param name="inputHint"></param>
        /// <returns></returns>
        public async Task ReplyWith(ITurnContext turnContext, string templateId, object data = null, string inputHint = InputHints.AcceptingInput)
        {
            BotAssert.ContextNotNull(turnContext);

            // apply template
            Activity boundActivity = await RenderTemplate(turnContext, turnContext.Activity?.AsMessageActivity()?.Locale, templateId, data, inputHint).ConfigureAwait(false);
            if (boundActivity != null)
            {
                await turnContext.SendActivityAsync(boundActivity);
                return;
            }
            return;
        }

        /// <summary>
        /// Render the template setting the speach (ssml) property to be same as text
        /// </summary>
        /// <param name="turnContext"></param>
        /// <param name="language"></param>
        /// <param name="templateId"></param>
        /// <param name="data"></param>
        /// <param name="inputHint"></param>
        /// <returns></returns>
        public async Task<Activity> RenderTemplate(ITurnContext turnContext, string language, string templateId, object data = null, string inputHint = InputHints.AcceptingInput)
        {
            var fallbackLocales = new List<string>(this.GetLanguagePolicy());

            if (!string.IsNullOrEmpty(language))
            {
                fallbackLocales.Add(language);
            }

            fallbackLocales.Add("default");

            // try each locale until successful
            foreach (var locale in fallbackLocales)
            {
                foreach (var renderer in this.List())
                {
                    object templateOutput = await renderer.RenderTemplate(turnContext, locale, templateId, data);
                    if (templateOutput != null)
                    {
                        if (templateOutput is string)
                        {
                            return MessageFactory.Text((string)templateOutput, ssml: (string)templateOutput, inputHint: inputHint);
                        }
                        else
                        {
                            var activityFromTemplate = templateOutput as Activity;
                            if (string.IsNullOrEmpty(activityFromTemplate.InputHint))
                            {
                                activityFromTemplate.InputHint = inputHint;
                            }
                            return activityFromTemplate;
                        }
                    }
                }
            }
            return null;
        }
    }
}

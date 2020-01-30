// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions
{
    /// <summary>
    /// Event prompt that enables Bots to wait for a incoming event matching a given name to be received.
    /// </summary>
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class EventPrompt : ActivityPrompt
    {
        public EventPrompt(string dialogId, string eventName, PromptValidator<Activity> validator)
            : base(dialogId, validator)
        {
            EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
        }

        public string EventName { get; set; }

        protected override Task<PromptRecognizerResult<Activity>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken)
        {
            var result = new PromptRecognizerResult<Activity>();
            var activity = turnContext.Activity;

            if (activity.Type == ActivityTypes.Event && !string.IsNullOrEmpty(activity.Name))
            {
                var ev = activity.AsEventActivity();

                if (ev.Name == EventName)
                {
                    result.Succeeded = true;
                    result.Value = turnContext.Activity;
                }
            }

            return Task.FromResult(result);
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;

    public class ConsoleOutputMiddleware : IMiddleware
    {
        public async Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.LogActivity(string.Empty, context.Activity);
            context.OnSendActivities(this.OnSendActivities);

            await next(cancellationToken).ConfigureAwait(false);
        }

        private static string GetTextOrSpeak(IMessageActivity messageActivity)
        {
            return string.IsNullOrWhiteSpace(messageActivity.Text) ? messageActivity.Speak : messageActivity.Text;
        }

        private async Task<ResourceResponse[]> OnSendActivities(ITurnContext context, List<Activity> activities, Func<Task<ResourceResponse[]>> next)
        {
            foreach (var response in activities)
            {
                this.LogActivity(string.Empty, response);
            }

            return await next();
        }

        private void LogActivity(string prefix, Activity contextActivity)
        {
            Console.WriteLine(string.Empty);
            if (contextActivity.Type == ActivityTypes.Message)
            {
                var messageActivity = contextActivity.AsMessageActivity();
                Console.WriteLine($"{prefix} [{DateTime.Now:ss.fff}] {GetTextOrSpeak(messageActivity)}");
            }
            else if (contextActivity.Type == ActivityTypes.Event)
            {
                var eventActivity = contextActivity.AsEventActivity();
                Console.WriteLine($"{prefix} Event: [{DateTime.Now:ss.fff}{DateTime.Now.Millisecond}] {eventActivity.Name}");
            }
            else
            {
                Console.WriteLine($"{prefix} {contextActivity.Type}: [{DateTime.Now:ss.fff}{DateTime.Now.Millisecond}]");
            }
        }
    }
}
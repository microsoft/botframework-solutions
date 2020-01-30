// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.TaskExtensions
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class QueuedHostedService : BackgroundService
    {
        public QueuedHostedService(IBackgroundTaskQueue queue)
        {
            TaskQueue = queue;
        }

        public IBackgroundTaskQueue TaskQueue { get; set; }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await TaskQueue.DequeueAsync(stoppingToken).ConfigureAwait(false);

                try
                {
                    await workItem(stoppingToken).ConfigureAwait(false);
                }
                catch
                {
                    // execution failed. added exception handling later
                }
            }
        }
    }
}
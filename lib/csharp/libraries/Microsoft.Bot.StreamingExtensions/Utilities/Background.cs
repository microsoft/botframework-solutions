// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.StreamingExtensions.Utilities
{
    internal static class Background
    {
#pragma warning disable IDE0022
        /// <summary>
        /// Register background task with ASP.Net hosting environment and trace exceptions
        /// Falls back to Thread pool if not running under ASP.Net.
        /// </summary>
        /// <param name="task">Background task to execute.</param>
        /// <param name="properties">Name value pairs to trace if an exception is thrown.</param>
        public static void Run(Func<Task> task, IDictionary<string, object> properties = null)
        {
            Run((ct) => task(), properties);
        }

        /// <summary>
        /// Register background task with ASP.Net hosting environment and trace exceptions
        /// Falls back to Thread pool if not running under ASP.Net.
        /// </summary>
        /// <param name="task">background task to execute.</param>
        /// <param name="properties">name value pairs to trace if an exception is thrown.</param>
        public static void Run(Func<CancellationToken, Task> task, IDictionary<string, object> properties = null)
        {
            Task.Run(() => TrackAsRequest(() => task(CancellationToken.None), properties));
        }

        /// <summary>
        /// Register periodic background task with ASP.Net hosting environment and trace exceptions.
        /// </summary>
        /// <param name="task">background task to execute.</param>
        /// <param name="spanDelay">the initial delay.</param>
        /// <param name="eventName">the event name to log individual execution failures.</param>
        public static void RunForever(Func<CancellationToken, TimeSpan> task, TimeSpan spanDelay, string eventName)
        {
            RunForever(token => Task.FromResult(task(token)), spanDelay, eventName);
        }

        /// <summary>
        /// Register periodic background task with ASP.Net hosting environment and trace exceptions.
        /// </summary>
        /// <param name="task">Background task to execute.</param>
        /// <param name="spanDelay">The initial delay.</param>
        /// <param name="eventName">The event name to log individual execution failures.</param>
        #pragma warning disable IDE0060
        public static void RunForever(Func<CancellationToken, Task<TimeSpan>> task, TimeSpan spanDelay, string eventName)
        {
            Background.Run(async token =>
            {
                try
                {
                    await Task.Delay(spanDelay, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // swallow these so we don't log exceptions on normal shutdown
                }

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        spanDelay = await task(token).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        await Task.Delay(spanDelay, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        // swallow these so we don't log exceptions on normal shutdown
                    }
                }
            });
        }
#pragma warning restore IDE0060

        #pragma warning disable IDE0060
        private static async Task TrackAsRequest(Func<Task> task, IDictionary<string, object> properties)
        {
            try
            {
                await task().ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }
#pragma warning restore IDE0060
#pragma warning restore IDE0022
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.TaskExtensions
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class ScheduledTaskModel
    {
        public string Name { get; set; }

        public string ScheduleExpression { get; set; }

        public Func<CancellationToken, Task> Task { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.TaskExtensions
{
    public class ScheduledTaskModel
    {
        public string Name { get; set; }

        public string ScheduleExpression { get; set; }

        public Func<CancellationToken, Task> Task { get; set; }

        public CancellationToken CancellationToken { get; set; }
    }
}
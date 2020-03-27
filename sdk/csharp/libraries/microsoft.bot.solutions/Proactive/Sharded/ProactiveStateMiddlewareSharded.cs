// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Proactive.Sharded
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Solutions.Util;

    /// <summary>
    /// A Middleware for saving the proactive model data
    /// This middleware will refresh user's latest conversation reference and save it to state.
    /// </summary>
    public class ProactiveStateMiddlewareSharded : ProactiveStateMiddleWareTemplate<ProactiveStateSharded>
    {
        public ProactiveStateMiddlewareSharded(ProactiveStateSharded proactiveStateSharded)
            : base(proactiveStateSharded)
        {
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Util;

namespace Microsoft.Bot.Solutions.Proactive
{
    /// <summary>
    /// A Middleware for saving the proactive model data
    /// This middleware will refresh user's latest conversation reference and save it to state.
    /// </summary>
    public class ProactiveStateMiddleware : ProactiveStateMiddleWareTemplate<ProactiveState>
    {
        public ProactiveStateMiddleware(ProactiveState proactiveStateSharded)
            : base(proactiveStateSharded)
        {
        }
    }
}
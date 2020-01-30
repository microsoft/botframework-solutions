// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Solutions.Middleware;

namespace Microsoft.Bot.Builder.Solutions.Testing
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class DefaultTestAdapter : TestAdapter
    {
        public DefaultTestAdapter()
            : base(sendTraceActivity: false)
        {
            Use(new EventDebuggerMiddleware());
        }
    }
}
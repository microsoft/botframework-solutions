// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Solutions.Middleware;

namespace Microsoft.Bot.Solutions.Testing
{
    public class DefaultTestAdapter : TestAdapter
    {
        public DefaultTestAdapter()
            : base(sendTraceActivity: false)
        {
            Use(new EventDebuggerMiddleware());
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions;

namespace Microsoft.Bot.Builder.Skills.Protocol
{
    public class RouteAction
    {
        public Func<ReceiveRequest, dynamic, CancellationToken, Task<object>> Action { get; set; }
    }
}

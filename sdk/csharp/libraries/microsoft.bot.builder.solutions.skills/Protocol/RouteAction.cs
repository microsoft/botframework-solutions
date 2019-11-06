// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Bot.Streaming;

namespace Microsoft.Bot.Builder.Skills.Protocol
{
    public class RouteAction
    {
        public Func<ReceiveRequest, dynamic, Task<object>> Action { get; set; }
    }
}
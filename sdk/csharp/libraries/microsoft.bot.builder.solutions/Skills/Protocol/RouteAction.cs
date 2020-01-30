// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Bot.Streaming;

namespace Microsoft.Bot.Builder.Solutions.Skills.Protocol
{
    [Obsolete("This type is being deprecated. To continue using Skill capability please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class RouteAction
    {
        public Func<ReceiveRequest, dynamic, Task<object>> Action { get; set; }
    }
}
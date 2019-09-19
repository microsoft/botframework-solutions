// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions;

namespace Microsoft.Bot.Builder.Skills.Protocol
{
    internal class RouteContext
    {
        public ReceiveRequest Request { get; set; }

        // TODO: try change this by a concrete type (IDictionary<string, object>)
        public dynamic RouteData { get; set; }

        public RouteAction ActionAsync { get; set; }
    }
}

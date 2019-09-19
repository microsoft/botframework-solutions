// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.StreamingExtensions;

namespace Microsoft.Bot.Builder.Skills.Protocol
{
    public class RouteContext
    {
        public ReceiveRequest Request { get; set; }

        // TODO: try change this by a concrete type (IDictionary<string, object>)
        public dynamic RouteData { get; set; }

        public RouteAction Action { get; set; }
    }
}

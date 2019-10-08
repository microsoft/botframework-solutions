﻿using Microsoft.Bot.StreamingExtensions;

namespace Microsoft.Bot.Builder.Skills.Protocol
{
    public class RouteContext
    {
        public ReceiveRequest Request { get; set; }

        public dynamic RouteData { get; set; }

        public RouteAction Action { get; set; }
    }
}
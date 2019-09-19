// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Builder.Skills.Protocol
{
    public class RouteTemplate
    {
        public string Method { get; set; }

        public string Path { get; set; }

        public RouteAction Action { get; set; }
    }
}

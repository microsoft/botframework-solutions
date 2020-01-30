// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Builder.Solutions.Skills.Protocol
{
    [Obsolete("This type is being deprecated. To continue using Skill capability please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class RouteTemplate
    {
        public string Method { get; set; }

        public string Path { get; set; }

        public RouteAction Action { get; set; }
    }
}
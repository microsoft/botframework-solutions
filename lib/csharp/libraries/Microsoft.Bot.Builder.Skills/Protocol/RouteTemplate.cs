// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.StreamingExtensions;

namespace Microsoft.Bot.Builder.Skills.Protocol
{
    public delegate Task<object> RouteAction(ReceiveRequest request, dynamic routeData, CancellationToken cancellationToken);

    internal class RouteTemplate
    {
        public string Method { get; set; }

        public string Path { get; set; }

        public RouteAction ActionAsync { get; set; }
    }
}

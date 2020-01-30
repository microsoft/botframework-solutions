// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    [Obsolete("This type is being deprecated. To continue using Skill capability please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public interface IServiceClientCredentials
    {
        string MicrosoftAppId { get; set; }

        Task<string> GetTokenAsync(bool forceRefresh = false);

        Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }
}
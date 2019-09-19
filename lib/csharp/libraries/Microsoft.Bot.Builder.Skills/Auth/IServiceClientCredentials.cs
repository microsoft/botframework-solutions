// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public interface IServiceClientCredentials
    {
        string MicrosoftAppId { get; set; }

        Task<string> GetTokenAsync(bool forceRefresh = false);

        Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }
}

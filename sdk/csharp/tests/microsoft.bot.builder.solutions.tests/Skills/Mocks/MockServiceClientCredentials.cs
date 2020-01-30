// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;

namespace Microsoft.Bot.Builder.Solutions.Tests.Skills.Mocks
{
    [Obsolete("This type is being deprecated.", false)]
    public class MockServiceClientCredentials : IServiceClientCredentials
    {
        public string MicrosoftAppId { get; set; } = Guid.NewGuid().ToString();

        public Task<string> GetTokenAsync(bool forceRefresh = false)
        {
            return Task.FromResult(string.Empty);
        }

        public Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
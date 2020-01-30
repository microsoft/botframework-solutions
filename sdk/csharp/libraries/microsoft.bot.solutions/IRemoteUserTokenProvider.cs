// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace Microsoft.Bot.Solutions
{
    /// <summary>
    /// Interface that represents remove invocation behavior.
    /// </summary>
    public interface IRemoteUserTokenProvider
    {
        Task SendRemoteTokenRequestEventAsync(ITurnContext turnContext, CancellationToken cancellationToken);
    }
}
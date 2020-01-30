// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions
{
    /// <summary>
    /// Interface that represents remove invocation behavior.
    /// </summary>
    [Obsolete("This type is being deprecated. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public interface IRemoteUserTokenProvider
    {
        Task SendRemoteTokenRequestEventAsync(ITurnContext turnContext, CancellationToken cancellationToken);
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions
{
    /// <summary>
    /// Interface that represents fallback request send behavior.
    /// </summary>
    public interface IFallbackRequestProvider
    {
         Task SendRemoteFallbackEventAsync(ITurnContext turnContext, CancellationToken cancellationToken);
    }
}

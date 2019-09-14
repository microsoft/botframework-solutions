// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    public interface ISkillResponseHandler
    {
        Task<ResourceResponse> SendActivityAsync(ITurnContext context, Activity activity, CancellationToken cancellationToken = default);

        Task<ResourceResponse> UpdateActivityAsync(ITurnContext context, Activity activity, CancellationToken cancellationToken = default);

        Task DeleteActivityAsync(ITurnContext context, string activityId, CancellationToken cancellationToken = default);
    }
}

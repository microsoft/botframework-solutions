// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    // TODO: Look at the BF SendActivitiesHandler for this

    /// <summary>
    /// A callback delegate for activities returned from a skill.
    /// </summary>
    /// <param name="turnContext">The turn context.</param>
    /// <param name="activity">The activity (as received from the skill).</param>
    /// <param name="cancellationToken">The task cancellation token.</param>
    /// <returns>An activity to be send back to the calling bot.</returns>
    /// <remarks>
    /// This delegate can be used to inspect an incoming activity or alter it if needed before it sends back to the turn context.
    /// </remarks>
    public delegate Task<Activity> SkillResponseProcessor(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken);
}

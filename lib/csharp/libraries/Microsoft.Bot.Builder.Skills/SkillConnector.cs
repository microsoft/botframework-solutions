// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// SkillConnector is the base class that handles communication with a skill.
    /// </summary>
    /// <remarks>
    /// Its responsibility is to forward a incoming request to the skill and handle
    /// the responses based on Skill Protocol.
    /// </remarks>
    public abstract class SkillConnector
    {
        // TODO: add delegate for interception.

        /// <summary>
        /// Forward incoming request to the skill.
        /// </summary>
        /// <param name="context">The <see cref="TurnContext"/> for the activity.</param>
        /// <param name="activity">Activity object to forward.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Response activity of the forwarded activity to the skill.</returns>
        public abstract Task<Activity> ForwardActivityAsync(ITurnContext context, Activity activity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancel the remote skill dialogs on the stack.
        /// </summary>
        /// <param name="turnContext">The turn context instance.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        public abstract Task CancelRemoteDialogsAsync(ITurnContext turnContext, CancellationToken cancellationToken = default);
    }
}

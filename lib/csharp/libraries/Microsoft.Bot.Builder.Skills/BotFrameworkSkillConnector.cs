// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// BotFrameworkSkillConnector that inherits from the base SkillConnector.
    /// </summary>
    /// <remarks>
    /// Its responsibility is to forward a incoming request to the skill and handle
    /// the responses based on Skill Protocol.
    /// </remarks>
    public class BotFrameworkSkillConnector : SkillConnector
    {
        private readonly SkillTransport _skillTransport;

        public BotFrameworkSkillConnector(SkillTransport skillTransport)
        {
            _skillTransport = skillTransport ?? throw new ArgumentNullException(nameof(skillTransport));
        }

        public override async Task<Activity> ForwardActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken = default)
        {
            return await _skillTransport.ForwardToSkillAsync(turnContext, activity, cancellationToken).ConfigureAwait(false);
        }

        public override async Task CancelRemoteDialogsAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await _skillTransport.CancelRemoteDialogsAsync(turnContext, cancellationToken).ConfigureAwait(false);
        }
    }
}

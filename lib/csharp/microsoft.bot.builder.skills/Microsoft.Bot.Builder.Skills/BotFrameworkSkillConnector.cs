// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Models;
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
        private readonly SkillConnectionConfiguration _skillConnectionConfiguration;
        private readonly SkillTransport _skillTransport;

        public BotFrameworkSkillConnector(SkillConnectionConfiguration skillConnectionConfiguration, SkillTransport skillTransport)
        {
            _skillConnectionConfiguration = skillConnectionConfiguration ?? throw new ArgumentNullException(nameof(skillConnectionConfiguration));
            _skillTransport = skillTransport ?? throw new ArgumentNullException(nameof(skillTransport));
        }

        public override async Task<Activity> ForwardToSkillAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken = default)
        {
            var response = await _skillTransport.ForwardToSkillAsync(turnContext, _skillConnectionConfiguration.SkillManifest, _skillConnectionConfiguration.ServiceClientCredentials, activity, this, cancellationToken).ConfigureAwait(false);
            _skillTransport.Disconnect();
            return response;
        }

        public override async Task CancelRemoteDialogsAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
            => await _skillTransport.CancelRemoteDialogsAsync(turnContext, _skillConnectionConfiguration.SkillManifest, _skillConnectionConfiguration.ServiceClientCredentials, cancellationToken).ConfigureAwait(false);

        public override async Task<ResourceResponse> SendActivityAsync(ITurnContext context, Activity activity, CancellationToken cancellationToken = default)
        {
            await context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
            return new ResourceResponse(activity.Id);
        }

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext context, Activity activity, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public override Task DeleteActivityAsync(ITurnContext context, string activityId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}

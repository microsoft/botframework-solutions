// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    public abstract class SkillTransport
    {
        public abstract Task<Activity> ForwardToSkillAsync(ITurnContext turnContext, SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, Activity activity, ISkillResponseHandler skillResponseHandler, CancellationToken cancellationToken = default);

        public abstract Task CancelRemoteDialogsAsync(ITurnContext turnContext, SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, CancellationToken cancellationToken = default);

        public abstract void Disconnect();
    }
}

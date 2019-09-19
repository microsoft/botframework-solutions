// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    // TODO: so far this class is very similar to BotFrameworkSkillConnector or SkillConnector, do we need the two?
    public abstract class SkillTransport
    {
        public abstract Task<Activity> ForwardToSkillAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken);

        public abstract Task CancelRemoteDialogsAsync(ITurnContext turnContext, CancellationToken cancellationToken);
    }
}

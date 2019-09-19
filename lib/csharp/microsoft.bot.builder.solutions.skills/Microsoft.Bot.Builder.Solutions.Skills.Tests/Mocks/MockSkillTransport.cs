// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Skills.Tests.Mocks
{
    public class MockSkillTransport : SkillTransport
    {
        private Activity _activityForwarded;

        public bool CheckIfSkillInvoked()
            => _activityForwarded != null;

        public void VerifyActivityForwardedCorrectly(Action<Activity> assertion)
            => assertion(_activityForwarded);

        public override Task<Activity> ForwardToSkillAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            _activityForwarded = activity;
            return Task.FromResult<Activity>(null);
        }

        public override Task CancelRemoteDialogsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

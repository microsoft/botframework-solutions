// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;
using Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Tests.Skills.Mocks
{
    public class MockSkillTransport : ISkillTransport
    {
        private Activity _activityForwarded;

		public Task CancelRemoteDialogsAsync(SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, ITurnContext turnContext)
		{
			return Task.CompletedTask;
		}

        public void Disconnect()
        {
        }

		public Task<Activity> ForwardToSkillAsync(SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, ITurnContext dialogContext, Activity activity, Action<Activity> tokenRequestHandler = null)
		{
			_activityForwarded = activity;

            return Task.FromResult<Activity>(null);
        }

        public bool CheckIfSkillInvoked()
        {
            return _activityForwarded != null;
        }

        public void VerifyActivityForwardedCorrectly(Action<Activity> assertion)
        {
            assertion(_activityForwarded);
        }
    }
}
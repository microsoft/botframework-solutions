using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class MockSkillTransport : ISkillTransport
    {
        private Activity _activityForwarded;

		public Task CancelRemoteDialogsAsync(SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, ITurnContext turnContext, CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}

        public void Disconnect()
        {
        }

		public Task<Activity> ForwardToSkillAsync(SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, ITurnContext dialogContext, Activity activity, Action<Activity> tokenRequestHandler = null, Action<Activity> fallbackHandler = null, CancellationToken cancellationToken = default)
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

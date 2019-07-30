using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
	public class MockSkillTransport : ISkillTransport
	{
		private Activity _activityForwarded;

		public Task CancelRemoteDialogsAsync(SkillManifest skillManifest, IServiceClientCredentials appCredentials, ITurnContext turnContext)
		{
			return Task.CompletedTask;
		}

		public void Disconnect()
		{
		}

		public Task<bool> ForwardToSkillAsync(SkillManifest skillManifest, IServiceClientCredentials appCredentials, ITurnContext dialogContext, Activity activity, Action<Activity> tokenRequestHandler = null)
		{
			_activityForwarded = activity;

			return Task.FromResult(true);
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
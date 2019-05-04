using System;
using System.Threading.Tasks;
using Microsoft.Bot.Protocol;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
	public class MockSkillTransport : ISkillTransport
	{
		private string _activityForwarded = string.Empty;

		public Task CancelRemoteDialogsAsync(ITurnContext turnContext)
		{
			return Task.CompletedTask;
		}

		public void Disconnect()
		{
		}

		public Task<bool> ForwardToSkillAsync(ITurnContext dialogContext, Activity activity, Action<Activity> tokenRequestHandler = null)
		{
			_activityForwarded = JsonConvert.SerializeObject(activity, SerializationSettings.BotSchemaSerializationSettings);

			return Task.FromResult(true);
		}

		public bool CheckIfSkillInvoked()
		{
			return !string.IsNullOrWhiteSpace(_activityForwarded);
		}

		public void VerifyActivityForwardedCorrectly(Func<string, bool> assertion)
		{
			Assert.IsTrue(assertion(_activityForwarded));
		}
	}
}
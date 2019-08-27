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

        public void Disconnect()
        {
        }

        public Task ForwardToSkillAsync(SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, ITurnContext dialogContext, Activity activity)
        {
            _activityForwarded = activity;

            return Task.CompletedTask;
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